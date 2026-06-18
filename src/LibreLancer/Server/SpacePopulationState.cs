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
        public List<PatrolPathSegment>? Path;
        public int PathIndex = -1;
    }

    private readonly record struct PatrolPathSegment(Zone Zone, int Label);

    private sealed class PopGroup(ZoneState state, EncounterInfo encounter)
    {
        public readonly ZoneState State = state;
        public readonly EncounterInfo Encounter = encounter;
        public readonly List<GameObject> Ships = [];
        public readonly HashSet<string> PopulationClasses = new(System.StringComparer.OrdinalIgnoreCase);
        public double AgeSeconds;
        public string? ArrivalObject;
        public float PersistDistance;
        public int PathIndex = state.PathIndex;
        public Vector3? InitialPathTarget;
        public bool InCombat;
        public Vector3? ResumeDutyTarget;
    }

    private readonly record struct SpawnLocation(
        Vector3 Position,
        Quaternion Orientation,
        string? ArrivalObject,
        int ArrivalIndex,
        int PathIndex = -1,
        Vector3? InitialPathTarget = null,
        float PersistDistance = 0);

    private readonly record struct PopulationContext(GameObject[] Players, int Density);
}
