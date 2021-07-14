using System.Threading.Tasks;

namespace LibreLancer.Net
{
    public partial class RemoteServerPlayer
    {
        private IPacketConnection connection;
        private CGameSession session;
        public RemoteServerPlayer(IPacketConnection connection, CGameSession session)
        {
            this.connection = connection;
            this.session = session;
        }
        void SendPacket(IPacket packet)
        {
            connection.SendPacket(packet, PacketDeliveryMethod.ReliableOrdered);
        }
        
        TaskCompletionSource<int> GetCompletionSource_int(int seq)
        {
            return session.ResponseHandler.GetCompletionSource_int(seq);
        }
    }
}