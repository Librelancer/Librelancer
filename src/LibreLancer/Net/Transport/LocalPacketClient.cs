// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using Lidgren.Network;

namespace LibreLancer
{
    public class LocalPacketClient : IPacketClient
    {
        public ConcurrentQueue<IPacket> Packets = new ConcurrentQueue<IPacket>();
        public void SendPacket(IPacket packet, NetDeliveryMethod method)
        {
            Packets.Enqueue(packet);
        }
    }
}