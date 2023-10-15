// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Net.Protocol;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LibreLancer.Net
{
    public class RemotePacketClient : IPacketClient
    {
        public NetPeer Client;
        public NetHpidWriter Hpids;

        public void SendPacket(IPacket packet, PacketDeliveryMethod method)
        {
            method.ToLiteNetLib(out DeliveryMethod mt, out byte ch);
            var m = new PacketWriter(new NetDataWriter(), Hpids);
            Packets.Write(m, packet);
            Client.Send(m, ch, mt);
        }

        public void Disconnect(DisconnectReason reason)
        {
            var pw = new PacketWriter();
            pw.Put(reason);
            Client.Disconnect(pw);
        }


        public void SendPacketWithEvent(IPacket packet, Action onAck, PacketDeliveryMethod method)
        {
            var m = new PacketWriter();
            Packets.Write(m, packet);
            method.ToLiteNetLib(out var mtd, out var channel);
            Client.SendWithDeliveryEvent(m, channel, mtd, onAck);
        }

        public RemotePacketClient(NetPeer client, NetHpidWriter hpids)
        {
            Client = client;
            this.Hpids = hpids;
        }
    }
}
