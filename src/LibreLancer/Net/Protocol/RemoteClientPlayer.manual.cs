using System.Threading.Tasks;


namespace LibreLancer.Net
{
    public partial class RemoteClientPlayer : IClientPlayer
    {
        private Player srv;
        public RemoteClientPlayer(Player player)
        {
            srv = player;
        }
        public void SendPacket(IPacket packet)
        {
            srv.Client.SendPacket(packet, PacketDeliveryMethod.ReliableOrdered);
        }

        TaskCompletionSource<int> GetCompletionSource_int(int retSeq)
        {
            return srv.ResponseHandler.GetCompletionSource_int(retSeq);
        }
        
        TaskCompletionSource<bool> GetCompletionSource_bool(int retSeq)
        {
            return srv.ResponseHandler.GetCompletionSource_bool(retSeq);
        }
        
    }
}