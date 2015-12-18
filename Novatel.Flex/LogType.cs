using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Novatel.Flex
{
    public enum LogType : ushort
    {
        /// <summary>
        /// List of system logs
        /// </summary>
        LogList = 5,
        /// <summary>
        /// Gps ephemeeris data
        /// </summary>
        GpsEphem = 7,
        Version = 37,
        /// <summary>
        /// Satelite visibility
        /// </summary>
        SatVis = 72,
        /// <summary>
        /// Self-test status
        /// </summary>
        RxStatus = 93,
        /// <summary>
        /// Velocity data
        /// </summary>
        BestVel = 99,
        /// <summary>
        /// Pseudorange velocity information
        /// </summary>
        PsrVel = 100,
        /// <summary>
        /// Receiver time information
        /// </summary>
        Time = 101,
        Passcom1 = 233,
        Passcom2 = 234,
        Passcom3 = 235,
        BestPos = 42,
        IpStatus = 1289,
        EthStatus = 1288,
        IpStats = 1669,

    }
}
