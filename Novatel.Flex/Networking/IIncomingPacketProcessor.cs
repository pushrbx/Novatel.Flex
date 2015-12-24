namespace Novatel.Flex.Networking
{
    internal interface IIncomingPacketProcessor : IPacketProcessor
    {
        void Process(Packet p);
    }
}
