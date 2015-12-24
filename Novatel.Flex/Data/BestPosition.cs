using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Novatel.Flex.Data
{
    public class BestPosition : INovatelData
    {
        public double LongitudeDegrees { get; set; }
        public double LatitudeDegrees { get; set; }
        public double HeightDegrees { get; set; }

        public float Latidue { get; set; }

        public float Longitude { get; set; }
    }
}
