using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Novatel.Flex
{
    internal class DeviceClient
    {
        private readonly TcpClient m_tcpClient;
        private bool m_isRunning;

        public DeviceClient(IPAddress ip, int port)
        {
            m_tcpClient = new TcpClient(new IPEndPoint(ip, port));
        }

        public void StartReceive(double interval)
        {
            if (!m_tcpClient.Connected)
                return;

            if (m_isRunning)
                return;

            m_isRunning = true;

            var stream = m_tcpClient.GetStream();
            // todo: start a new thread.
        }
    }
}
