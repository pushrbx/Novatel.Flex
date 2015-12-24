using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Novatel.Flex.Networking;
using Novatel.Flex.Networking.Data;
using Novatel.Flex.Networking.Processors;

namespace Novatel.Flex
{
    internal class DeviceClient
    {
        protected TcpClient m_tcpClient;
        protected readonly Adapter m_adapter;
        protected NetworkStream m_remoteStream;
        protected TransferBuffer m_remoteReceiveBuffer;
        private bool m_isRunning;
        protected CancellationTokenSource m_tokenSource;
        protected List<Packet> m_remoteIncomingPackets;
        protected List<KeyValuePair<TransferBuffer, Packet>> m_remoteOutgoingPackets;
        private bool m_shutdown;

        protected Task m_workerThread;

        private IPAddress Ip { get; set; }

        private int Port { get; set; }

        public DeviceClient(IPAddress ip, int port)
        {
            Ip = ip;
            Port = port;
            m_adapter = new Adapter((byte)PortIndentifier.Eth1All);
            m_tokenSource = new CancellationTokenSource();
            m_tokenSource.Token.Register(OnWorkerThreadCancellation);
        }

        protected virtual void OnWorkerThreadCancellation()
        {
            if (m_tcpClient == null)
                return;

            // cancel the flow
            var m = new RequestSingleBestPos();
            m_shutdown = true;
            m_adapter.Send(m.CreatePacket());
            Thread.Sleep(500);
            m_tcpClient.Close();
        }

        public void StartConnection()
        {
            m_workerThread = Task.Run((Action)WorkingThread, m_tokenSource.Token);
        }

        private void WorkingThread()
        {
            try
            {
                m_remoteReceiveBuffer = new TransferBuffer(0x8000, 0, 0);
                m_tcpClient = new TcpClient();
                m_tcpClient.Connect(Ip, Port);
                m_remoteStream = m_tcpClient.GetStream();

                while (true)
                {
                    m_tokenSource.Token.ThrowIfCancellationRequested();

                    if (m_remoteStream.DataAvailable)
                    {
                        m_remoteReceiveBuffer.Offset = 0;
                        m_remoteReceiveBuffer.Size = m_remoteStream.Read(m_remoteReceiveBuffer.Buffer, 0,
                            m_remoteReceiveBuffer.Buffer.Length);
                        m_adapter.Receive(m_remoteReceiveBuffer);
                    }

                    m_remoteIncomingPackets = m_adapter.GetIncomingPackets();
                    if (m_remoteIncomingPackets != null && !m_shutdown)
                    {
                        foreach (var packet in m_remoteIncomingPackets)
                        {
                            PacketRouter.Default.Process(packet);
                        }
                    }

                    m_remoteOutgoingPackets = m_adapter.GetOutgoingPackets();
                    if (m_remoteOutgoingPackets != null)
                    {
                        foreach (var kvp in m_remoteOutgoingPackets)
                        {
                            //var packet = kvp.Value;
                            var buffer = kvp.Key;
                            m_remoteStream.Write(buffer.Buffer, 0, buffer.Size);
                        }
                    }

                    Thread.Sleep(1);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                throw new NovatelNetworkException("Error occured in the connection with the gps unit. See inner exception for details.", ex);
            }
        }

        public void CloseConnection()
        {
            if (m_workerThread.IsCanceled || m_workerThread.IsCompleted || m_workerThread.IsFaulted)
                return;

            m_tokenSource.Cancel(true);
        }

        public void SendPacket(Packet p)
        {
            m_adapter.Send(p);
        }

        public void SendRaw(byte[] buffer)
        {
            m_remoteStream.Write(buffer, 0, buffer.Length);
        }
    }
}
