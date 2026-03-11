// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Diagnostics.CodeAnalysis;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Net
{
    public interface IPacketConnection : IPacketSender
    {
        bool PollPacket([MaybeNullWhen(false)] out IPacket packet);
        uint EstimateTickDelay();
        void Shutdown();
    }
}
