// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LiteNetLib;

namespace LibreLancer
{
    public interface IPacketClient
    {
        void SendPacket(IPacket packet, PacketDeliveryMethod method);
        void SendPacketWithEvent(IPacket packet, Action onAck, PacketDeliveryMethod method);
    }
}