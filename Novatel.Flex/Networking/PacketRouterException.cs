using System;
using System.Runtime.Serialization;

namespace Novatel.Flex.Networking
{
    /// <summary>
    /// Description of PacketRouterException
    /// </summary>
    public class PacketRouterException : Exception, ISerializable
    {
        public PacketRouterException()
        {
        }

        public PacketRouterException(string message)
            : base(message)
        {
        }

        public PacketRouterException(string message, PacketRouterException innerException)
            : base(message, innerException)
        {
        }

        // This constructor is needed for serialization.
        protected PacketRouterException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}