// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Net
{
    public interface IPacketClient
    {
        void SendPacket(IPacket packet, PacketDeliveryMethod method, bool force = false);
        void SendPacketWithEvent(IPacket packet, Action onAck, PacketDeliveryMethod method);
    }
}