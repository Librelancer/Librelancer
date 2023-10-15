// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Net
{
    public class LocalPacketClient : IPacketClient
    {
        public ConcurrentQueue<IPacket> Packets = new ConcurrentQueue<IPacket>();
        public void SendPacket(IPacket packet, PacketDeliveryMethod method)
        {
            #if DEBUG
            Net.Protocol.Packets.CheckRegistered(packet);
            #endif
            Packets.Enqueue(packet);
        }

        public void SendPacketWithEvent(IPacket packet, Action onAck, PacketDeliveryMethod method)
        {
            #if DEBUG
            Net.Protocol.Packets.CheckRegistered(packet);
            #endif
            Packets.Enqueue(packet);
            onAck();
        }

        public void Disconnect(DisconnectReason reason) => throw new InvalidOperationException($"Tried to disconnect SP with reason {reason}");
    }
}
