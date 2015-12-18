using System.IO;
using Novatel.Flex.Utilities;

namespace Novatel.Flex.Networking
{
    internal class PacketReader : EndianBinaryReader
    {
        public PacketReader(byte[] input)
            : base(EndianBitConverter.Big, new MemoryStream(input, false))
        {
        }

        public PacketReader(byte[] input, int index, int count)
            : base(EndianBitConverter.Big, new MemoryStream(input, index, count, false))
        {
        }
    }
}