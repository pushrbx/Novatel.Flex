using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novatel.Flex.Data;

namespace Novatel.Flex.Networking.Processors
{
    internal class BestPosProcesssor : IIncomingPacketProcessor
    {
        private BestPosition m_current;

        public void Run()
        {
            NovatelFacade.ExecuteCallback(m_current);
        }

        public void Process(Packet p)
        {
            if (p.MessageId != (ushort) LogType.BestPos)
                return;

            p.SeekRead(p.GetHeaderLength(), SeekOrigin.Begin);
            var solStat = p.ReadUInt32();
            var posType = p.ReadUInt32();
            var latDegrees = p.ReadDouble();
            var lonDegrees = p.ReadDouble();
            var hight = p.ReadDouble();
            var undulation = p.ReadSingle();
            var datumId = p.ReadUInt32();
            var latDeviation = p.ReadSingle();
            var lonDeviation = p.ReadSingle();
            var heightDeviation = p.ReadSingle();
            var stationId = p.ReadUInt8Array(4);
            var diffAge = p.ReadSingle();
            var solAge = p.ReadSingle();
            var numberOfSatelites = p.ReadUInt8();
            var usedSatelites = p.ReadUInt8();
            m_current = new BestPosition()
            {
                LatitudeDegrees = latDegrees,
                LongitudeDegrees = lonDegrees,
                Latidue = latDeviation,
                Longitude = lonDeviation
            };
        }
    }
}
