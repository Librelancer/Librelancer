using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.World;
using Zone = LibreLancer.Data.GameData.World.Zone;

namespace LibreLancer.Server;

public partial class SpacePopulationManager
{
    private sealed class ZoneState(Zone zone)
    {
        public readonly Zone Zone = zone;
        public readonly List<PopGroup> Groups = [];
        public readonly Dictionary<EncounterFormation, int> FormationCreateCounts = [];
        public double TimeUntilHeartbeat;
        public double BattleCooldown;
        public bool InBattle => BattleCooldown > 0;
        public List<Zone>? Path;
        public int PathIndex = -1;
    }

    private sealed class PopGroup(ZoneState state, EncounterInfo encounter)
    {
        public readonly ZoneState State = state;
        public readonly EncounterInfo Encounter = encounter;
        public readonly List<GameObject> Ships = [];
        public double AgeSeconds;
        public string? ArrivalObject;
    }

    private readonly record struct SpawnLocation(
        Vector3 Position,
        Quaternion Orientation,
        string? ArrivalObject,
        int ArrivalIndex);
}
