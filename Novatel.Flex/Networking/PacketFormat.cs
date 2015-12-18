using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Novatel.Flex.Networking
{
    internal enum PacketFormat : byte
    {
        Binary = 0,
        Ascii = 1,
        AbbreviatedAcii = 2,
        Reserved = 3
    }
}
