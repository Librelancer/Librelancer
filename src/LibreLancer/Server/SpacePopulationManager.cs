using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.Missions;
using LibreLancer.Missions.Directives;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;
using UniverseEncounter = LibreLancer.Data.Schema.Universe.Encounter;
using Zone = LibreLancer.Data.GameData.World.Zone;

namespace LibreLancer.Server;

public class SpacePopulationManager
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

    private GameObject[] GetPlayerObjects() =>
        world.Players.Values
            .Where(x => (x.Flags & GameObjectFlags.Exists) == GameObjectFlags.Exists)
            .ToArray();

    private void TryRepopulate(ZoneState state, GameObject[] players)
    {
        var alive = CountShips(state);
        if (state.InBattle && state.Zone.MaxBattleSize > 0 && alive >= state.Zone.MaxBattleSize)
            return;

        if (alive >= state.Zone.Density)
            return;

        var repopTime = Math.Max(1, state.Zone.RepopTime);
        var chance = Math.Clamp((state.Zone.Density - alive) / (float)repopTime, 0, 1);
        if (random.NextSingle() > chance)
            return;

        if (!TryChooseEncounter(state.Zone, out var encounterDef))
            return;

        var faction = ChooseFaction(encounterDef);
        if (faction == null)
            return;

        var encounterIni = GetEncounterIni(encounterDef.Archetype);
        if (encounterIni == null)
            return;

        var info = EncounterHandler.CreateEncounter(
            encounterIni,
            encounterDef.Difficulty,
            faction,
            world.Server.GameData.Items,
            random);
        if (info.Ships.Count == 0)
            return;
        if (state.InBattle &&
            state.Zone.MaxBattleSize > 0 &&
            alive + info.Ships.Count > state.Zone.MaxBattleSize)
        {
            return;
        }
        if (!CanCreateFormation(state, info))
            return;

        var creationDistance = info.FormationDefinition?.ZoneCreationDistance ?? 0;
        if (!TryFindSpawnLocation(state, info, players, creationDistance, out var spawn))
            return;

        SpawnGroup(state, info, faction, spawn);
    }

    private int CountShips(ZoneState state) =>
        state.Groups.Sum(x => x.Ships.Count(Alive));

    private bool TryChooseEncounter(Zone zone, out UniverseEncounter encounter)
    {
        encounter = null!;
        if (zone.Encounters is not { Length: > 0 })
            return false;

        var total = zone.Encounters.Sum(x => Math.Max(0, x.Chance));
        if (total <= 0)
        {
            encounter = zone.Encounters[random.Next(zone.Encounters.Length)];
            return true;
        }

        var roll = total <= 1
            ? random.NextDouble()
            : random.NextDouble() * total;
        var cursor = 0.0;
        foreach (var item in zone.Encounters)
        {
            cursor += Math.Max(0, item.Chance);
            if (roll <= cursor)
            {
                encounter = item;
                return true;
            }
        }
        return false;
    }

    private Faction? ChooseFaction(UniverseEncounter encounter)
    {
        if (encounter.FactionSpawns.Count == 0)
            return null;

        var total = encounter.FactionSpawns.Sum(x => Math.Max(0, x.Chance));
        if (total <= 0)
        {
            foreach (var spawn in encounter.FactionSpawns)
            {
                var faction = world.Server.GameData.Items.Factions.Get(spawn.Faction);
                if (faction != null)
                    return faction;
            }
            return null;
        }

        var roll = total <= 1
            ? random.NextDouble()
            : random.NextDouble() * total;
        foreach (var spawn in encounter.FactionSpawns)
        {
            roll -= Math.Max(0, spawn.Chance);
            if (roll > 0)
                continue;

            var faction = world.Server.GameData.Items.Factions.Get(spawn.Faction);
            if (faction != null)
                return faction;
        }
        return null;
    }

    private EncounterIni? GetEncounterIni(string archetype)
    {
        if (encounterCache.TryGetValue(archetype, out var cached))
            return cached;

        var parameters = world.System.EncounterParameters.FirstOrDefault(x =>
            archetype.Equals(x.Nickname, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(parameters.Nickname))
        {
            FLLog.Warning("SpacePop", $"Encounter parameters {archetype} not found in {world.System.Nickname}");
            encounterCache[archetype] = null;
            return null;
        }

        var path = world.Server.GameData.Items.DataPath(parameters.SourceFile);
        if (path == null)
        {
            encounterCache[archetype] = null;
            return null;
        }

        try
        {
            return encounterCache[archetype] = new EncounterIni(path, world.Server.GameData.Items.VFS);
        }
        catch (Exception ex)
        {
            FLLog.Warning("SpacePop", $"Could not parse encounter {parameters.SourceFile}: {ex.Message}");
            encounterCache[archetype] = null;
            return null;
        }
    }

    private void SpawnGroup(ZoneState state, EncounterInfo info, Faction faction, SpawnLocation spawn)
    {
        var items = world.Server.GameData.Items;
        var position = spawn.Position;
        var firstTarget = GetFirstDirectiveTarget(state, info, position);
        var orientation = spawn.ArrivalObject != null
            ? spawn.Orientation
            : LookRotation(firstTarget - position);
        var group = new PopGroup(state, info)
        {
            ArrivalObject = spawn.ArrivalObject
        };

        for (int i = 0; i < info.Ships.Count; i++)
        {
            var entry = info.Ships[i];
            if (string.IsNullOrWhiteSpace(entry.Ship.Loadout) ||
                !items.TryGetLoadout(entry.Ship.Loadout, out var loadout))
            {
                FLLog.Warning("SpacePop", $"NPC ship {entry.Ship.Nickname} has no valid loadout");
                continue;
            }

            var stateGraph = string.IsNullOrWhiteSpace(entry.Ship.StateGraph)
                ? "FIGHTER"
                : entry.Ship.StateGraph;
            var pilot = string.IsNullOrWhiteSpace(entry.Ship.Pilot) ? null : items.GetPilot(entry.Ship.Pilot);
            var nickname = $"spacepop_{++spawnCounter}_{i}";
            var offset = spawn.ArrivalObject == null
                ? GetSpawnOffset(info, i, orientation)
                : Vector3.Zero;
            var costume = RandomCostume(faction);
            var obj = world.NPCs.DoSpawn(
                entry.Name,
                nickname,
                faction,
                stateGraph,
                costume,
                loadout,
                pilot,
                position + offset,
                orientation,
                spawn.ArrivalObject,
                spawn.ArrivalIndex,
                null,
                false);
            group.Ships.Add(obj);
        }

        if (group.Ships.Count == 0)
            return;

        state.Groups.Add(group);
        RecordFormationCreation(state, info);
        if (info.Formation != null && group.Ships.Count > 1)
        {
            FormationTools.MakeNewFormation(
                group.Ships[0],
                world.GameWorld,
                info.Formation.Nickname,
                group.Ships.Skip(1).Select(x => x.Nickname).ToList());
        }
        AssignDirectives(group);
    }

    private bool CanCreateFormation(ZoneState state, EncounterInfo info)
    {
        var formation = info.FormationDefinition;
        if (formation == null)
            return true;

        if (!formation.AllowSimultaneousCreation && state.Groups.Any(x =>
                ReferenceEquals(x.Encounter.FormationDefinition, formation) &&
                x.Ships.Any(Alive)))
        {
            return false;
        }

        if (!TryGetTimesToCreate(formation, out var limit))
            return true;

        return state.FormationCreateCounts.GetValueOrDefault(formation) < limit;
    }

    private static void RecordFormationCreation(ZoneState state, EncounterInfo info)
    {
        var formation = info.FormationDefinition;
        if (formation == null || !TryGetTimesToCreate(formation, out _))
            return;

        state.FormationCreateCounts[formation] =
            state.FormationCreateCounts.GetValueOrDefault(formation) + 1;
    }

    private static bool TryGetTimesToCreate(EncounterFormation formation, out int limit)
    {
        limit = 0;
        var value = formation.TimesToCreate;
        if (string.IsNullOrWhiteSpace(value) ||
            value.Equals("infinite", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(value, out limit) && limit >= 0;
    }

    private CostumeEntry? RandomCostume(Faction faction)
    {
        if (faction.Properties?.SpaceCostume.Count > 0)
        {
            var costume = faction.Properties.SpaceCostume[random.Next(faction.Properties.SpaceCostume.Count)];
            return new CostumeEntry([costume.Head, costume.Body, costume.Extra], world.Server.GameData.Items);
        }
        return null;
    }

    private static Vector3 GetSpawnOffset(EncounterInfo info, int index, Quaternion orientation)
    {
        if (info.Formation != null && index < info.Formation.Positions.Count)
            return Vector3.Transform(info.Formation.Positions[index], orientation);

        if (index == 0)
            return Vector3.Zero;

        var angle = (MathF.PI * 2f / Math.Max(2, index + 1)) * index;
        return Vector3.Transform(new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * 75f, orientation);
    }

    private void AssignDirectives(PopGroup group)
    {
        var leader = group.Ships.FirstOrDefault(Alive);
        if (leader == null)
            return;

        var directives = BuildDirectives(group, leader.WorldTransform.Position);
        if (directives.Length == 0)
            return;

        var leadOnly = group.Encounter.Formation != null && group.Ships.Count > 1;
        foreach (var ship in group.Ships)
        {
            if (!Alive(ship))
                continue;
            if (leadOnly && ship != leader)
                continue;

            ship.GetComponent<DirectiveRunnerComponent>()?.SetDirectives(directives, world.GameWorld);
        }
    }

    private MissionDirective[] BuildDirectives(PopGroup group, Vector3 currentPosition)
    {
        if (ShouldRetire(group))
            return BuildRetirementDirectives(group.State, currentPosition);

        var behavior = group.Encounter.FormationDefinition?.Behavior ?? EncounterBehavior.wander;
        return behavior switch
        {
            EncounterBehavior.patrol_path => BuildPathDirectives(group.State, currentPosition, GotoKind.GotoCruise, 100),
            EncounterBehavior.trade when IsPatrol(group.State.Zone) => BuildPathDirectives(group.State, currentPosition, GotoKind.GotoCruise, 100),
            EncounterBehavior.trade => BuildTradeDirectives(group.State, currentPosition, group.ArrivalObject),
            _ => BuildWanderDirectives(group.State.Zone)
        };
    }

    private static bool ShouldRetire(PopGroup group)
    {
        var reliefTime = group.State.Zone.ReliefTime;
        if (reliefTime <= 0 || group.AgeSeconds < reliefTime)
            return false;

        var behavior = group.Encounter.FormationDefinition?.Behavior ?? EncounterBehavior.wander;
        return behavior == EncounterBehavior.patrol_path || IsPatrol(group.State.Zone);
    }

    private MissionDirective[] BuildRetirementDirectives(ZoneState state, Vector3 currentPosition)
    {
        var dockable = FindDockable(currentPosition);
        if (!string.IsNullOrWhiteSpace(dockable?.Nickname))
        {
            return
            [
                new GotoShipDirective
                {
                    Target = dockable.Nickname!,
                    CruiseKind = GotoKind.GotoCruise,
                    Range = 750,
                    MaxThrottle = 100
                },
                new DockDirective { Target = dockable.Nickname! }
            ];
        }

        return BuildPathDirectives(state, currentPosition, GotoKind.GotoCruise, 100);
    }

    private MissionDirective[] BuildWanderDirectives(Zone zone)
    {
        var count = random.Next(2, 5);
        var directives = new MissionDirective[count];
        for (int i = 0; i < directives.Length; i++)
        {
            directives[i] = new GotoVecDirective
            {
                Target = SampleZonePoint(zone),
                CruiseKind = GotoKind.GotoNoCruise,
                Range = 500,
                MaxThrottle = 80
            };
        }
        return directives;
    }

    private MissionDirective[] BuildTradeDirectives(ZoneState state, Vector3 currentPosition, string? excludeDockable)
    {
        var dockable = FindDockable(currentPosition, excludeDockable);
        if (!string.IsNullOrWhiteSpace(dockable?.Nickname))
        {
            return
            [
                new GotoShipDirective
                {
                    Target = dockable.Nickname!,
                    CruiseKind = GotoKind.GotoCruise,
                    Range = 750,
                    MaxThrottle = 100
                },
                new DockDirective { Target = dockable.Nickname! }
            ];
        }

        return BuildPathDirectives(state, currentPosition, GotoKind.GotoCruise, 100);
    }

    private MissionDirective[] BuildPathDirectives(
        ZoneState state,
        Vector3 currentPosition,
        GotoKind kind,
        float throttle)
    {
        var targets = GetPathTargets(state, currentPosition);
        if (targets.Count == 0)
            targets.Add(SampleZonePoint(state.Zone));

        return targets.Select(x => (MissionDirective)new GotoVecDirective
        {
            Target = x,
            CruiseKind = kind,
            Range = 750,
            MaxThrottle = throttle
        }).ToArray();
    }

    private List<Vector3> GetPathTargets(ZoneState state, Vector3 currentPosition)
    {
        var result = new List<Vector3>();
        if (state.Path == null || state.PathIndex < 0)
            return result;

        var direction = 1;
        if (state.PathIndex + direction >= state.Path.Count)
            direction = -1;

        for (int i = state.PathIndex + direction; i >= 0 && i < state.Path.Count && result.Count < 4; i += direction)
        {
            result.Add(state.Path[i].Position);
        }

        if (result.Count == 0)
        {
            var zone = state.Path
                .OrderBy(x => Vector3.DistanceSquared(x.Position, currentPosition))
                .FirstOrDefault(x => x != state.Zone);
            if (zone != null)
                result.Add(zone.Position);
        }

        return result;
    }

    private GameObject? FindDockable(Vector3 currentPosition, string? excludeNickname = null)
    {
        GameObject? nearest = null;
        var nearestDistance = float.MaxValue;
        foreach (var obj in world.GameWorld.Objects)
        {
            if (obj.SystemObject == null ||
                (obj.Flags & GameObjectFlags.Exists) != GameObjectFlags.Exists ||
                !obj.TryGetComponent<SDockableComponent>(out _))
            {
                continue;
            }
            if (!string.IsNullOrWhiteSpace(excludeNickname) &&
                excludeNickname.Equals(obj.Nickname, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var distance = Vector3.DistanceSquared(currentPosition, obj.WorldTransform.Position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = obj;
            }
        }
        return nearest;
    }

    private Vector3 GetFirstDirectiveTarget(ZoneState state, EncounterInfo info, Vector3 position)
    {
        var behavior = info.FormationDefinition?.Behavior ?? EncounterBehavior.wander;
        if (behavior == EncounterBehavior.patrol_path || IsPatrol(state.Zone))
        {
            var targets = GetPathTargets(state, position);
            if (targets.Count > 0)
                return targets[0];
        }
        return SampleZonePoint(state.Zone);
    }

    private void UpdateIdleGroupDirectives(ZoneState state)
    {
        foreach (var group in state.Groups)
        {
            var leader = group.Ships.FirstOrDefault(Alive);
            if (leader == null)
                continue;
            var runner = leader.GetComponent<DirectiveRunnerComponent>();
            if (runner is { Active: false })
                AssignDirectives(group);
        }
    }

    private void UpdateBattleState(ZoneState state, GameObject[] players, double delta)
    {
        if (players.Length > 0 && state.Groups.Any(x => GroupInCombat(x, players)))
        {
            state.BattleCooldown = BattleCooldownSeconds;
            return;
        }

        state.BattleCooldown = Math.Max(0, state.BattleCooldown - delta);
    }

    private bool GroupInCombat(PopGroup group, GameObject[] players)
    {
        foreach (var ship in group.Ships)
        {
            if (!Alive(ship))
                continue;
            if (ShipHasCombatTarget(ship, players) || ShipHasHostilePlayerNearby(ship, players))
                return true;
        }
        return false;
    }

    private static bool ShipHasCombatTarget(GameObject ship, GameObject[] players)
    {
        var selected = ship.GetComponent<SelectedTargetComponent>()?.Selected;
        if (selected != null &&
            Alive(selected) &&
            (selected.TryGetComponent<SPlayerComponent>(out _) ||
             selected.TryGetComponent<SRepComponent>(out _)))
        {
            return true;
        }

        foreach (var player in players)
        {
            if (player.GetComponent<SPlayerComponent>()?.SelectedObject == ship)
                return true;
        }
        return false;
    }

    private static bool ShipHasHostilePlayerNearby(GameObject ship, GameObject[] players)
    {
        if (!ship.TryGetComponent<SRepComponent>(out var rep))
            return false;

        foreach (var player in players)
        {
            if (Vector3.DistanceSquared(ship.WorldTransform.Position, player.WorldTransform.Position) <=
                BattleDistance * BattleDistance &&
                rep.IsHostileTo(player))
            {
                return true;
            }
        }
        return false;
    }

    private void PruneAndDespawn(GameObject[] players)
    {
        foreach (var state in zones)
        {
            for (int i = state.Groups.Count - 1; i >= 0; i--)
            {
                var group = state.Groups[i];
                group.Ships.RemoveAll(x => !Alive(x));
                if (group.Ships.Count == 0)
                {
                    state.Groups.RemoveAt(i);
                    continue;
                }

                var persistDistance = state.InBattle || GroupInCombat(group, players)
                    ? BattlePersistDistance
                    : DefaultPersistDistance;
                if (players.Length == 0 || group.Ships.All(x => DistanceToNearestPlayer(x.WorldTransform.Position, players) > persistDistance))
                {
                    foreach (var ship in group.Ships)
                    {
                        if (Alive(ship))
                            world.RemoveSpawnedObject(ship, false);
                    }
                    state.Groups.RemoveAt(i);
                }
            }
        }
    }

    private bool TryFindSpawnLocation(
        ZoneState state,
        EncounterInfo info,
        GameObject[] players,
        float zoneCreationDistance,
        out SpawnLocation spawn)
    {
        spawn = default;
        var arrival = info.FormationDefinition?.Arrival;
        if (TryFindArrivalObject(state.Zone, arrival, players, zoneCreationDistance, out var arrivalObject))
        {
            spawn = new SpawnLocation(
                arrivalObject.WorldTransform.Position,
                arrivalObject.WorldTransform.Orientation,
                arrivalObject.Nickname,
                0);
            return true;
        }

        if (!ArrivalAllowsFreeSpace(arrival))
            return false;

        if (!TryFindSpawnPoint(state.Zone, players, zoneCreationDistance, out var point))
            return false;

        spawn = new SpawnLocation(point, Quaternion.Identity, null, 0);
        return true;
    }

    private bool TryFindArrivalObject(
        Zone zone,
        EncounterArrival? arrival,
        GameObject[] players,
        float zoneCreationDistance,
        out GameObject arrivalObject)
    {
        arrivalObject = null!;
        if (players.Length == 0 || !ArrivalAllowsObjects(arrival))
            return false;

        var maxDistance = zoneCreationDistance > 0 ? zoneCreationDistance : DefaultSpawnMaxDistance;
        var searchDistance = Math.Max(maxDistance * 2.5f, DefaultPersistDistance);
        var bestScore = float.MaxValue;

        foreach (var obj in world.GameWorld.Objects)
        {
            if (string.IsNullOrWhiteSpace(obj.Nickname) ||
                obj.SystemObject == null ||
                !Alive(obj) ||
                !obj.TryGetComponent<SDockableComponent>(out var dockable) ||
                dockable.DockPoints.Length == 0 ||
                !ObjectMatchesArrival(obj, dockable, arrival))
            {
                continue;
            }

            var distance = DistanceToNearestPlayer(obj.WorldTransform.Position, players);
            if (distance > searchDistance)
                continue;

            var score = distance + (zone.ContainsPoint(obj.WorldTransform.Position) ? 0 : DefaultSpawnMaxDistance);
            score += random.NextSingle() * 250f;
            if (score < bestScore)
            {
                bestScore = score;
                arrivalObject = obj;
            }
        }

        return arrivalObject != null;
    }

    private static bool ArrivalAllowsObjects(EncounterArrival? arrival)
    {
        if (arrival == null)
            return false;
        if (ArrivalExcluded(arrival, Arrivals.all))
            return false;
        return ArrivalIncluded(arrival, Arrivals.all) ||
               ArrivalIncluded(arrival, Arrivals.object_all) ||
               ArrivalIncluded(arrival, Arrivals.tradelane) ||
               ArrivalIncluded(arrival, Arrivals.object_docking_ring) ||
               ArrivalIncluded(arrival, Arrivals.object_jump_gate) ||
               ArrivalIncluded(arrival, Arrivals.object_station) ||
               ArrivalIncluded(arrival, Arrivals.object_capital);
    }

    private static bool ArrivalAllowsFreeSpace(EncounterArrival? arrival)
    {
        if (arrival == null || arrival.Includes.Count == 0)
            return true;
        if (ArrivalExcluded(arrival, Arrivals.all))
            return false;
        if (ArrivalIncluded(arrival, Arrivals.all))
            return !ArrivalExcluded(arrival, Arrivals.cruise) ||
                   !ArrivalExcluded(arrival, Arrivals.buzz);
        return (ArrivalIncluded(arrival, Arrivals.cruise) && !ArrivalExcluded(arrival, Arrivals.cruise)) ||
               (ArrivalIncluded(arrival, Arrivals.buzz) && !ArrivalExcluded(arrival, Arrivals.buzz));
    }

    private static bool ObjectMatchesArrival(GameObject obj, SDockableComponent dockable, EncounterArrival? arrival)
    {
        if (arrival == null || ArrivalExcluded(arrival, Arrivals.all))
            return false;

        var isTradelane = dockable.Action.Kind == DockKinds.Tradelane;
        if (!isTradelane && ArrivalExcluded(arrival, Arrivals.object_all))
            return false;

        var kinds = ObjectArrivalKinds(obj, dockable).ToArray();
        if (kinds.Any(x => ArrivalExcluded(arrival, x)))
            return false;

        if (ArrivalIncluded(arrival, Arrivals.all))
            return true;
        if (!isTradelane && ArrivalIncluded(arrival, Arrivals.object_all))
            return true;

        return kinds.Any(x => ArrivalIncluded(arrival, x));
    }

    private static IEnumerable<Arrivals> ObjectArrivalKinds(GameObject obj, SDockableComponent dockable)
    {
        if (dockable.Action.Kind == DockKinds.Tradelane)
        {
            yield return Arrivals.tradelane;
            yield break;
        }

        var type = obj.SystemObject?.Archetype?.Type ?? ArchetypeType.NONE;
        switch (type)
        {
            case ArchetypeType.docking_ring:
                yield return Arrivals.object_docking_ring;
                break;
            case ArchetypeType.jump_gate:
            case ArchetypeType.jump_hole:
            case ArchetypeType.jumphole:
                yield return Arrivals.object_jump_gate;
                break;
            case ArchetypeType.station:
            case ArchetypeType.weapons_platform:
                yield return Arrivals.object_station;
                break;
        }

        if (IsCapitalObject(obj))
            yield return Arrivals.object_capital;
    }

    private static bool IsCapitalObject(GameObject obj)
    {
        var nickname = obj.SystemObject?.Archetype?.Nickname ?? obj.Nickname ?? string.Empty;
        return nickname.Contains("capital", StringComparison.OrdinalIgnoreCase) ||
               nickname.Contains("battleship", StringComparison.OrdinalIgnoreCase) ||
               nickname.Contains("cruiser", StringComparison.OrdinalIgnoreCase) ||
               nickname.Contains("dreadnought", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ArrivalIncluded(EncounterArrival arrival, Arrivals arrivalKind) =>
        arrival.Includes.Contains(arrivalKind);

    private static bool ArrivalExcluded(EncounterArrival arrival, Arrivals arrivalKind) =>
        arrival.Excludes.Contains(arrivalKind);

    private bool TryFindSpawnPoint(
        Zone zone,
        GameObject[] players,
        float zoneCreationDistance,
        out Vector3 point)
    {
        point = Vector3.Zero;
        var maxDistance = zoneCreationDistance > 0 ? zoneCreationDistance : DefaultSpawnMaxDistance;
        var minDistance = Math.Min(DefaultSpawnMinDistance, maxDistance * 0.5f);

        for (int i = 0; i < 64; i++)
        {
            var player = players[random.Next(players.Length)];
            var distance = Lerp(minDistance, maxDistance, random.NextSingle());
            var candidate = player.WorldTransform.Position + RandomUnitVector() * distance;
            if (zone.ContainsPoint(candidate))
            {
                point = candidate;
                return true;
            }
        }

        for (int i = 0; i < 32; i++)
        {
            var candidate = SampleZonePoint(zone);
            var distance = DistanceToNearestPlayer(candidate, players);
            if (distance <= maxDistance * 1.5f)
            {
                point = candidate;
                return true;
            }
        }

        return false;
    }

    private float DistanceToNearestPlayer(Vector3 point, GameObject[] players)
    {
        var nearest = float.MaxValue;
        foreach (var player in players)
        {
            var distance = Vector3.Distance(point, player.WorldTransform.Position);
            if (distance < nearest)
                nearest = distance;
        }
        return nearest;
    }

    private Vector3 SampleZonePoint(Zone zone)
    {
        for (int i = 0; i < 16; i++)
        {
            var candidate = SampleZonePointUnchecked(zone);
            if (zone.ContainsPoint(candidate))
                return candidate;
        }
        return zone.Position;
    }

    private Vector3 SampleZonePointUnchecked(Zone zone)
    {
        return zone.Shape switch
        {
            ShapeKind.Sphere => zone.Position + RandomUnitVector() * (zone.Size.X * MathF.Cbrt(random.NextSingle())),
            ShapeKind.Ellipsoid => TransformZoneLocal(zone, RandomUnitVector() * MathF.Cbrt(random.NextSingle()) * zone.Size),
            ShapeKind.Box => TransformZoneLocal(zone, new Vector3(
                (random.NextSingle() - 0.5f) * zone.Size.X,
                (random.NextSingle() - 0.5f) * zone.Size.Y,
                (random.NextSingle() - 0.5f) * zone.Size.Z)),
            ShapeKind.Cylinder => SampleCylinder(zone, 0),
            ShapeKind.Ring => SampleCylinder(zone, zone.Size.Z),
            _ => zone.Position
        };
    }

    private Vector3 SampleCylinder(Zone zone, float innerRadius)
    {
        var angle = random.NextSingle() * MathF.PI * 2f;
        var radius = MathF.Sqrt(Lerp(innerRadius * innerRadius, zone.Size.X * zone.Size.X, random.NextSingle()));
        var local = new Vector3(
            MathF.Cos(angle) * radius,
            (random.NextSingle() - 0.5f) * zone.Size.Y,
            MathF.Sin(angle) * radius);
        return TransformZoneLocal(zone, local);
    }

    private static Vector3 TransformZoneLocal(Zone zone, Vector3 local) =>
        zone.Position + Vector3.Transform(local, zone.RotationMatrix);

    private Vector3 RandomUnitVector()
    {
        var z = random.NextSingle() * 2f - 1f;
        var angle = random.NextSingle() * MathF.PI * 2f;
        var radius = MathF.Sqrt(MathF.Max(0, 1f - z * z));
        return new Vector3(MathF.Cos(angle) * radius, z, MathF.Sin(angle) * radius);
    }

    private static Quaternion LookRotation(Vector3 direction)
    {
        if (direction.LengthSquared() < 0.0001f)
            return Quaternion.Identity;

        direction = Vector3.Normalize(direction);
        var up = Math.Abs(Vector3.Dot(direction, Vector3.UnitY)) > 0.95f
            ? Vector3.UnitZ
            : Vector3.UnitY;
        return Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(
            Matrix4x4.CreateWorld(Vector3.Zero, -direction, up)));
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
