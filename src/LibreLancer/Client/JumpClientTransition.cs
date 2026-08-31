// MIT License - Copyright (c) LibreLancer contributors
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.GameData;
using LibreLancer.Net;

namespace LibreLancer.Client;

internal enum JumpClientPhase
{
    Docking,
    Tunnel,
    DestinationReady,
    Arrival
}

internal sealed class JumpClientTransition
{
    public required ObjNetId SourceGate;
    public required string DestinationSystem;
    public required string ExitObject;
    public required uint ExitSeed;
    public required JumpGateEffect? Effect;
    public required GateTunnel? Tunnel;
    public JumpClientPhase Phase;
    public bool ForcedArrival;
}
