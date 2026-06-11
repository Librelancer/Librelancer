using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
    private const float BattlePersistDistance = 7500f;
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
        var paths = zones
            .Where(x => x.Zone.PathLabel is { Length: >= 2 })
            .GroupBy(x => x.Zone.PathLabel![0], StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            var ordered = path
                .Select(x => x.Zone)
                .OrderBy(PathIndex)
                .ToList();
            foreach (var state in path)
            {
                state.Path = ordered;
                state.PathIndex = ordered.IndexOf(state.Zone);
            }
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
            if (!state.Zone.ContainsPoint(position) ||
                !AllowsPopulationSpawn(state.Zone, position))
            {
                continue;
            }

            activePlayers.Add(player);
            effectiveDensity = Math.Max(effectiveDensity, EffectivePopulationDensity(state.Zone, position));
        }

        return new PopulationContext(activePlayers.ToArray(), effectiveDensity);
    }

    private bool AllowsPopulationSpawn(Zone zone, Vector3 position)
    {
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

    private int EffectivePopulationDensity(Zone zone, Vector3 position)
    {
        var density = zone.Density;
        foreach (var other in world.System.Zones)
        {
            if (ReferenceEquals(other, zone))
                return density;

            if (other.PopulationAdditive == false && other.ContainsPoint(position))
                density = other.Density;
        }

        return density;
    }

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
