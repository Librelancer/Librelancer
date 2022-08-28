// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
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
        void SendPacket(IPacket packet, int channel)
        {
            if (channel > 2 || channel < 0) throw new ArgumentException(nameof(channel));
            connection.SendPacket(packet, PacketDeliveryMethod.ReliableOrdered + channel);
        }

        private NetResponseHandler ResponseHandler => session.ResponseHandler;
    }
}