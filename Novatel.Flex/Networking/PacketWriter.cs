using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novatel.Flex.Utilities;

namespace Novatel.Flex.Networking
{
    internal class PacketWriter : EndianBinaryWriter
    {
        public PacketWriter()
            : base(EndianBitConverter.Big, new MemoryStream())
        {
        }

        public byte[] GetBytes()
        {
            var mem = (MemoryStream) BaseStream;
            return mem.GetBuffer();
        }
    }
}
