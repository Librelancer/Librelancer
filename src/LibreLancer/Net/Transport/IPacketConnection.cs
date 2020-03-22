using Lidgren.Network;

namespace LibreLancer
{
    public interface IPacketConnection
    {
        void SendPacket(IPacket packet, NetDeliveryMethod method);
        bool PollPacket(out IPacket packet);
        void Shutdown();
    }
}