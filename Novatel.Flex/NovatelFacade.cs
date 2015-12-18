using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novatel.Flex.Data;

namespace Novatel.Flex
{
    public static class NovatelFacade
    {
        public static bool Init()
        {
            
        }

        public static void RegisterCallback(Action<BestPosition> callback)
        {
            
        }

        public static TData GetSinglePacket<TData>() where TData : class
        {
            CheckType(typeof(TData));
        }

        private static void CheckType(Type type)
        {
            
        }
    }
}
