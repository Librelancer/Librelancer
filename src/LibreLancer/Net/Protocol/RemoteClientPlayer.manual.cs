// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
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

        private NetResponseHandler ResponseHandler => srv.ResponseHandler;

    }
}