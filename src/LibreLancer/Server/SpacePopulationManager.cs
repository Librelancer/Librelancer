using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Server.Components;
using LibreLancer.World;
using Zone = LibreLancer.Data.GameData.World.Zone;

namespace LibreLancer.Server;

public partial class SpacePopulationManager
{
    private const double HeartbeatSeconds = 3.0;
    private const float DefaultSpawnMaxDistance = 1775f;
    private const float DefaultSpawnMinDistance = 100f;
    private const float DefaultPersistDistance = 2500f;
    private const float BattleDistance = 5000f;
    private const float CombatEngageDistance = 2500f;
    private const float BattlePersistDistance = 7500f;
    private const float ZoneSpawnEdgeDistance = 2000f;
    private const float RandomMissionNoSpawnRadius = 10000f;
    private const double BattleCooldownSeconds = 10.0;

    private readonly ServerWorld world;
    private readonly Random random = new();
    private readonly List<ZoneState> zones = [];
    private readonly Dictionary<string, EncounterIni?> encounterCache = new(StringComparer.OrdinalIgnoreCase);
    private int spawnCounter;

    public SpacePopulationManager(ServerWorld world)
    {
        this.world = world;

        foreach (var zone in world.System.Zones)
        {
            if (zone.Encounters is not { Length: > 0 } || zone.Density <= 0)
                continue;

            zones.Add(new ZoneState(zone)
            {
                TimeUntilHeartbeat = random.NextDouble() * HeartbeatSeconds
            });
        }

        InitializePaths();
    }

    public void Update(double delta)
    {
        var players = GetPlayerObjects();
        foreach (var state in zones)
            UpdateBattleState(state, players, delta);

        PruneAndDespawn(players);

        if (players.Length == 0)
            return;

        foreach (var state in zones)
        {
            foreach (var group in state.Groups)
                group.AgeSeconds += delta;

            UpdateGroupCombat(state);
            UpdateIdleGroupDirectives(state);
            state.TimeUntilHeartbeat -= delta;
            if (state.TimeUntilHeartbeat > 0)
                continue;

            state.TimeUntilHeartbeat += HeartbeatSeconds;
            TryRepopulate(state, players);
        }
    }

    private void InitializePaths()
    {
        var paths = world.System.Zones
            .Where(x => x.PathLabel is { Length: >= 2 })
            .SelectMany(PatrolPathSegments)
            .GroupBy(x => x.Zone.PathLabel![0], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.OrderBy(y => y.Label).ToList(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var state in zones)
        {
            if (state.Zone.PathLabel is not { Length: >= 2 } ||
                !paths.TryGetValue(state.Zone.PathLabel[0], out var path))
                continue;

            state.Path = path;
            var zoneLabel = PathIndex(state.Zone);
            state.PathIndex = path.FindIndex(x =>
                ReferenceEquals(x.Zone, state.Zone) &&
                x.Label == zoneLabel);
        }
    }

    private static IEnumerable<PatrolPathSegment> PatrolPathSegments(Zone zone)
    {
        if (zone.PathLabel is not { Length: >= 2 })
            yield break;

        for (int i = 1; i < zone.PathLabel.Length; i++)
        {
            if (int.TryParse(zone.PathLabel[i], out var label))
                yield return new PatrolPathSegment(zone, label);
        }
    }

    private GameObject[] GetPlayerObjects() =>
        world.Players.Values
            .Where(x =>
                (x.Flags & GameObjectFlags.Exists) == GameObjectFlags.Exists &&
                IsFreetimePlayer(x))
            .ToArray();

    private static bool IsFreetimePlayer(GameObject obj) =>
        obj.TryGetComponent<SPlayerComponent>(out var player) &&
        player.Player.AllowFreetimePopulation;

    private PopulationContext GetPopulationContext(ZoneState state, GameObject[] players)
    {
        var activePlayers = new List<GameObject>();
        var effectiveDensity = 0;
        foreach (var player in players)
        {
            var position = player.WorldTransform.Position;
            if (!IsPopulationZoneActive(state, position) ||
                !AllowsPopulationSpawn(state.Zone, position))
            {
                continue;
            }

            activePlayers.Add(player);
            effectiveDensity = Math.Max(effectiveDensity, state.Zone.Density);
        }

        return new PopulationContext(activePlayers.ToArray(), effectiveDensity);
    }

    private bool IsPopulationZoneActive(ZoneState state, Vector3 position)
    {
        if (!state.Zone.ContainsPoint(position) &&
            DistanceToZoneEdge(state.Zone, position) > ZoneSpawnEdgeDistance)
        {
            return false;
        }

        return true;
    }

    private static float DistanceToZoneEdge(Zone zone, Vector3 position)
    {
        if (zone.ContainsPoint(position))
            return 0;

        var offset = position - zone.Position;
        return zone.Shape switch
        {
            ShapeKind.Sphere => MathF.Max(0, offset.Length() - zone.Size.X),
            ShapeKind.Box => DistanceToBoxEdge(zone, position),
            ShapeKind.Ellipsoid => DistanceToEllipsoidEdge(zone, position),
            ShapeKind.Cylinder or ShapeKind.Ring => DistanceToCylinderEdge(zone, position),
            _ => float.MaxValue
        };
    }

    private static float DistanceToBoxEdge(Zone zone, Vector3 position)
    {
        var local = Vector3.Transform(position - zone.Position, Matrix4x4.Transpose(zone.RotationMatrix));
        var outside = Vector3.Max(Vector3.Abs(local) - zone.Size * 0.5f, Vector3.Zero);
        return outside.Length();
    }

    private static float DistanceToEllipsoidEdge(Zone zone, Vector3 position)
    {
        var local = Vector3.Transform(position - zone.Position, Matrix4x4.Transpose(zone.RotationMatrix));
        var length = local.Length();
        if (length < 1)
            return 0;

        var direction = local / length;
        var denominator = MathF.Sqrt(
            (direction.X * direction.X) / (zone.Size.X * zone.Size.X) +
            (direction.Y * direction.Y) / (zone.Size.Y * zone.Size.Y) +
            (direction.Z * direction.Z) / (zone.Size.Z * zone.Size.Z));
        return denominator <= 0 ? 0 : MathF.Max(0, length - (1 / denominator));
    }

    private static float DistanceToCylinderEdge(Zone zone, Vector3 position)
    {
        var local = Vector3.Transform(position - zone.Position, Matrix4x4.Transpose(zone.RotationMatrix));
        var radial = MathF.Max(0, MathF.Sqrt(local.X * local.X + local.Z * local.Z) - zone.Size.X);
        var vertical = MathF.Max(0, MathF.Abs(local.Y) - zone.Size.Y * 0.5f);
        return MathF.Sqrt(radial * radial + vertical * vertical);
    }

    private bool AllowsPopulationSpawn(Zone zone, Vector3 position)
    {
        if (IsInsideRandomMissionNoSpawnZone(position))
            return false;

        if (zone.PopulationAdditive != false)
            return true;

        foreach (var other in world.System.Zones)
        {
            if (ReferenceEquals(other, zone))
                return true;
            if (other.ContainsPoint(position))
                return false;
        }

        return true;
    }

    private bool IsInsideRandomMissionNoSpawnZone(Vector3 position)
    {
        var radiusSquared = RandomMissionNoSpawnRadius * RandomMissionNoSpawnRadius;
        foreach (var player in world.Players.Keys)
        {
            if (player.ActiveRandomMissionPosition is not { } missionPosition)
                continue;
            if (Vector3.DistanceSquared(position, missionPosition) <= radiusSquared)
                return true;
        }
        return false;
    }

    private GameObject[] ActiveRandomMissionPlayerObjects() =>
        world.Players
            .Where(x =>
                x.Key.ActiveRandomMissionPosition.HasValue &&
                Alive(x.Value))
            .Select(x => x.Value)
            .ToArray();

    private static bool Alive(GameObject obj) =>
        (obj.Flags & GameObjectFlags.Exists) == GameObjectFlags.Exists;

    private static bool IsPatrol(Zone zone) =>
        zone.Usage?.Any(x => x.Equals("patrol", StringComparison.OrdinalIgnoreCase)) == true ||
        zone.PathLabel is { Length: >= 2 };

    private static int PathIndex(Zone zone) =>
        zone.PathLabel is { Length: >= 2 } && int.TryParse(zone.PathLabel[1], out var index)
            ? index
            : 0;

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
