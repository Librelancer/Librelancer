// MIT License - Copyright (c) LibreLancer contributors
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Server;

internal sealed class JumpTransferGuard
{
    [Flags]
    private enum State : byte
    {
        DestinationReady = 1,
        ClientReady = 2,
        SpawnScheduled = 4,
        Spawned = 8,
        Completed = 16
    }

    private readonly object sync = new();
    private State state;

    private bool Has(State value)
    {
        lock (sync)
            return (state & value) != 0;
    }

    private void Set(State value)
    {
        lock (sync)
            state |= value;
    }

    public bool Spawned => Has(State.Spawned);
    public bool Completed => Has(State.Completed);

    public void DestinationReady() => Set(State.DestinationReady);
    public void ClientReady() => Set(State.ClientReady);

    public bool TryScheduleSpawn(bool force = false)
    {
        lock (sync)
        {
            if ((state & State.SpawnScheduled) != 0 ||
                (state & State.DestinationReady) == 0 ||
                (!force && (state & State.ClientReady) == 0))
                return false;
            state |= State.SpawnScheduled;
            return true;
        }
    }

    private bool Advance(State required, State next)
    {
        lock (sync)
        {
            if ((state & required) != required || (state & next) != 0)
                return false;
            state |= next;
            return true;
        }
    }

    public bool MarkSpawned() => Advance(State.SpawnScheduled, State.Spawned);
    public bool TryComplete() => Advance(State.Spawned, State.Completed);
}
