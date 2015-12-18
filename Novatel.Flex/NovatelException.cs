using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Novatel.Flex
{
    public class NovatelException : Exception
    {
        public NovatelException(string msg) : base(msg)
        {
            
        }
    }

    public class NovatelNetworkException : Exception
    {
        public NovatelNetworkException(string msg)
            : base(msg)
        {

        }
    }
}
