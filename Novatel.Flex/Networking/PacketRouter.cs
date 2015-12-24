using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novatel.Flex.Networking.Processors;

namespace Novatel.Flex.Networking
{
    internal sealed class PacketRouter
    {
        private static PacketRouter _instance;
        private static readonly object _instanceLock = new object();
        private readonly ConcurrentDictionary<LogType, Func<IIncomingPacketProcessor>> m_handlers;

        public static PacketRouter Default
        {
            get
            {
                lock (_instanceLock)
                {
                    return _instance ?? (_instance = new PacketRouter());
                }
            }
        }

        private PacketRouter()
        {
            m_handlers = new ConcurrentDictionary<LogType, Func<IIncomingPacketProcessor>>();
        }

        private T PacketInstantiator<T>() where T : IIncomingPacketProcessor, new()
        {
            var packetHandler = new T();
            return packetHandler;
        }

        public void Process(Packet packet)
        {
            if (packet == null)
                throw new ArgumentNullException("packet");

            var opcode = (LogType) packet.MessageId;
            if (!m_handlers.ContainsKey(opcode)) return;
            Func<IIncomingPacketProcessor> temp;
            if (!m_handlers.TryGetValue(opcode, out temp)) return; // todo: implement retry
            var processor = temp();
            processor.Process(packet);
            processor.Run();
        }

        public void RegisterDefaultHandlers()
        {
            try
            {
                RegisterHandler(LogType.Log, PacketInstantiator<LogCommandProcessor>);
            }
            catch (Exception)
            {
                // ignore for now
            }
        }

        public bool RegisterHandler(LogType operationCode, Func<IIncomingPacketProcessor> handler, bool overwrite = false)
        {
            if (handler == null)
                throw new ArgumentException("Handler can not be null.", "handler");

            if (m_handlers.ContainsKey(operationCode) && !overwrite)
                throw new PacketRouterException(
                    string.Format("Handler for this operation code already exists. Operation code = {0:x2}",
                        (ushort) operationCode));

            if (overwrite && m_handlers.ContainsKey(operationCode))
            {
                Func<IIncomingPacketProcessor> temp;
                return m_handlers.TryGetValue(operationCode, out temp) &&
                       m_handlers.TryUpdate(operationCode, handler, temp);
            }

            return m_handlers.TryAdd(operationCode, handler);
        }
    }
}
