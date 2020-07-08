// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using LiteNetLib;

namespace LibreLancer
{
    public interface IPacketConnection
    {
        void SendPacket(IPacket packet, DeliveryMethod method);
        bool PollPacket(out IPacket packet);
        void Shutdown();
    }
}