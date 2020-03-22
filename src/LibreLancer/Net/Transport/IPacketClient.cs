using Lidgren.Network;

namespace LibreLancer
{
    public interface IPacketClient
    {
        void SendPacket(IPacket packet, NetDeliveryMethod method);
    }
}