using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Novatel.Flex
{
    internal class PacketException : Exception
    {
        public PacketException(string msg) : base(msg)
        { }
    }
}
