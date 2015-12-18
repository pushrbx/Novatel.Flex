using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Novatel.Flex.Networking.Data
{
    // todo: complete this
    // http://www.novatel.com/assets/Documents/Manuals/om-20000129.pdf  page 27
    internal enum PortIndentifier : byte
    {
        NoPorts = 0,
        Com1All = 1,
        Com2All = 2,
        Com3All = 3,
        ThisPortAll = 6,
        FileAll = 7,
        AllPorts = 8,
        XCom1All = 9,
        XCom2All = 10,
        Usb1All = 13,
        Usb2All = 14,
        Usb3All = 15,
        AuxAll = 16,
        XCom3All = 17,
        Com4All = 19,
        Eth1All = 20,
        ImuAll = 21,
        ICom1All = 23,
        ICom2All = 24,
    }
}
