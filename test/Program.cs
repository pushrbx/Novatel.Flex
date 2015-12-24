using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Novatel.Flex;
using Novatel.Flex.Data;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            NovatelFacade.RegisterCallback(x => Callback(x as BestPosition));
            NovatelFacade.Init(IPAddress.Parse("192.168.1.28"), 2000);
            Thread.Sleep(5000);
            NovatelFacade.RequestBestPos();
            Console.ReadLine();
        }

        static void Callback(BestPosition p)
        {
            if (p == null)
                return;
            Console.WriteLine("-------------------");
            Console.WriteLine(p.Longitude);
            Console.WriteLine(p.Latidue);
        }
    }
}
