// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using Lidgren.Network;

namespace LibreLancer
{
    public class RemotePacketClient : IPacketClient
    {
        public NetConnection Client;
        public NetServer Server;

        public void SendPacket(IPacket packet, NetDeliveryMethod method)
        {
            var m = Server.CreateMessage();
            m.Write(packet);
            Server.SendMessage(m, Client, method);
        }

        public RemotePacketClient(NetConnection client, NetServer server)
        {
            Client = client;
            Server = server;
        }
    }
}