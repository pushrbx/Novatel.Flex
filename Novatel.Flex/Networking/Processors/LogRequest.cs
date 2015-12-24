using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novatel.Flex.Networking.Data;

namespace Novatel.Flex.Networking.Processors
{
    internal class LogRequest : IOutgoingPacketFactory
    {
        public void Run()
        {
        }

        public Packet CreatePacket()
        {
            var packet = new Packet(LogType.Log, (byte)PortIndentifier.Eth1All);
            packet.WriteUInt32((byte)PortIndentifier.Eth1All);
            packet.WriteUInt16((ushort)LogType.BestPos);
            packet.WriteInt8(packet.MessageType);
            packet.WriteInt8(0);
            packet.WriteUInt32(2); // on time
            packet.WriteDouble(1.0); // period
            packet.WriteDouble(0); // offset
            packet.WriteUInt32(0); // hold

            return packet;
        }
    }

    internal class RequestSingleBestPos : IOutgoingPacketFactory
    {
        public void Run()
        {
        }

        public Packet CreatePacket()
        {
            var packet = new Packet(LogType.Log, (byte)PortIndentifier.Eth1All);
            packet.WriteUInt32((byte)PortIndentifier.Eth1All);
            packet.WriteUInt16((ushort)LogType.BestPos);
            packet.WriteInt8(packet.MessageType);
            packet.WriteInt8(0);
            packet.WriteUInt32(0); // on time
            packet.WriteDouble(0); // period
            packet.WriteDouble(0); // offset
            packet.WriteUInt32(0); // hold

            return packet;
        }
    }
}
