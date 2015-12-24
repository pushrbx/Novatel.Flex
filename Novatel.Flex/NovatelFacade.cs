using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Novatel.Flex.Data;
using Novatel.Flex.Networking.Processors;

namespace Novatel.Flex
{
    public static class NovatelFacade
    {
        private static readonly Dictionary<Type, Action<INovatelData>> _registry = new Dictionary<Type, Action<INovatelData>>();
        private static DeviceClient _client;

        public static void Init(IPAddress ip, int port)
        {
            _client = new DeviceClient(ip, port);
            _client.StartConnection();
        }

        public static void RegisterCallback(Action<INovatelData> callback)
        {
            if (_registry.ContainsKey(typeof (BestPosition)))
            {
                return;
            }

            _registry.Add(typeof(BestPosition), callback);
        }

        public static TData GetSinglePacket<TData>() where TData : INovatelData
        {
            CheckType(typeof(TData));

            return default(TData);
        }

        private static void CheckType(Type type)
        {
            
        }

        public static void RequestBestPos()
        {
            var m = new LogRequest();
            _client.SendPacket(m.CreatePacket());
        }

        internal static void ExecuteCallback<T>(T model) where T : INovatelData
        {
            if (_registry.ContainsKey(model.GetType()))
            {
                _registry[model.GetType()](model);
            }
        }
    }
}
