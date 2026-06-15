// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Threading;
using LibreLancer.Net.Protocol;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LibreLancer.Net
{
    public class RemotePacketClient : IPacketClient
    {
        public NetPeer Client;

        //LiteNetLib overhead = 4 bytes
        //NetPeer already includes IPv4/6 header size in reported Mtu
        public int MaxSequencedSize => (Client?.Mtu ?? 1024) - 4;

        private NetDataWriter writer = new();
        private Lock writerLock = new();

        public void SendPacket(IPacket packet, PacketDeliveryMethod method)
        {
            method.ToLiteNetLib(out DeliveryMethod mt, out byte ch);
            lock (writerLock)
            {
                writer.Reset();
                var m = new PacketWriter(writer);
                Packets.Write(m, packet);
                Client.Send(m, ch, mt);
            }
        }

        public void Disconnect(DisconnectReason reason)
        {
            lock (writerLock)
            {
                writer.Reset();
                var pw = new PacketWriter(writer);
                pw.Put(reason);
                Client.Disconnect(pw);
            }
        }

        public void SendPacketWithEvent(IPacket packet, Action onAck, PacketDeliveryMethod method)
        {
            method.ToLiteNetLib(out var mtd, out var channel);
            lock (writerLock)
            {
                writer.Reset();
                var m = new PacketWriter(writer);
                Packets.Write(m, packet);
                Client.SendWithDeliveryEvent(m, channel, mtd, onAck);
            }
        }

        public RemotePacketClient(NetPeer client)
        {
            Client = client;
        }
    }
}
