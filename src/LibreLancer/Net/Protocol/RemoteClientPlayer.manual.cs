// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Server;

namespace LibreLancer.Net.Protocol
{
    public partial class RemoteClientPlayer : IClientPlayer
    {
        private Player srv;
        public RemoteClientPlayer(Player player)
        {
            srv = player;
        }
        public void SendPacket(IPacket packet, int channel)
        {
            if (channel > 2 || channel < 0) throw new ArgumentException(nameof(channel));
            srv.Client.SendPacket(packet, PacketDeliveryMethod.ReliableOrdered + channel);
        }

        private Protocol.NetResponseHandler ResponseHandler => srv.ResponseHandler;

    }
}