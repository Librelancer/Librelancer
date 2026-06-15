using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.Server.Components;
using LibreLancer.World;
using Zone = LibreLancer.Data.GameData.World.Zone;

namespace LibreLancer.Server;

[Flags]
public enum ArrivalTargets
{
    None = 0,
    Tradelane = 1 << 1,
    DockingRing = 1 << 2,
    JumpGate = 1 << 3,
    Station = 1 << 4,
    Capital = 1 << 5,
    Cruise = 1 << 6,
    Buzz = 1 << 7,
    Objects = Tradelane | DockingRing | JumpGate | Station | Capital,
    All = Objects | Cruise | Buzz
}

public partial class SpacePopulationManager
{
    private static ArrivalTargets TranslateArrival(EncounterArrival? arrival)
    {
        if (arrival == null)
            return ArrivalTargets.None;

        ArrivalTargets allow = ArrivalTargets.None;
        foreach (var a in arrival.Includes)
        {
            allow |= ConvertArrival(a);
        }

        ArrivalTargets disallow = ArrivalTargets.None;
        foreach (var a in arrival.Excludes)
        {
            disallow |= ConvertArrival(a);
        }

        return allow & ~disallow;
    }

    private static ArrivalTargets ConvertArrival(Arrivals arrival) => arrival switch
    {
        Arrivals.all => ArrivalTargets.All,
        Arrivals.object_all => ArrivalTargets.Objects,
        Arrivals.tradelane => ArrivalTargets.Tradelane,
        Arrivals.object_docking_ring => ArrivalTargets.DockingRing,
        Arrivals.object_jump_gate => ArrivalTargets.JumpGate,
        Arrivals.object_station => ArrivalTargets.Station,
        Arrivals.object_capital => ArrivalTargets.Capital,
        Arrivals.cruise => ArrivalTargets.Cruise,
        Arrivals.buzz => ArrivalTargets.Buzz,
        _ => ArrivalTargets.None
    };

    private bool TryFindSpawnLocation(
        ZoneState state,
        EncounterInfo info,
        GameObject[] players,
        float zoneCreationDistance,
        bool allowCloseSpawn,
        out SpawnLocation spawn)
    {
        spawn = default;
        var arrivalTargets = TranslateArrival(info.FormationDefinition?.Arrival);
        var preferObjectArrival = info.FormationDefinition?.Behavior == EncounterBehavior.trade;
        if (!preferObjectArrival &&
            TryFindFreeSpaceSpawnLocation(state.Zone, arrivalTargets, players, zoneCreationDistance, allowCloseSpawn, out spawn))
        {
            return true;
        }

        if (TryFindArrivalObject(
            state.Zone,
            arrivalTargets,
            players,
            zoneCreationDistance,
            allowCloseSpawn,
            out var arrivalObject,
            out var arrivalIndex))
        {
            spawn = new SpawnLocation(
                arrivalObject.WorldTransform.Position,
                arrivalObject.WorldTransform.Orientation,
                arrivalObject.Nickname,
                arrivalIndex);
            return true;
        }

        if (preferObjectArrival &&
            TryFindFreeSpaceSpawnLocation(state.Zone, arrivalTargets, players, zoneCreationDistance, allowCloseSpawn, out spawn))
        {
            return true;
        }

        return false;
    }

    private bool TryFindFreeSpaceSpawnLocation(
        Zone zone,
        ArrivalTargets targets,
        GameObject[] players,
        float zoneCreationDistance,
        bool allowCloseSpawn,
        out SpawnLocation spawn)
    {
        spawn = default;
        if ((targets & (ArrivalTargets.Cruise | ArrivalTargets.Buzz)) == 0)
            return false;

        if (!TryFindSpawnPoint(zone, players, zoneCreationDistance, allowCloseSpawn, out var point))
            return false;

        spawn = new SpawnLocation(point, Quaternion.Identity, null, 0);
        return true;
    }

    private bool TryFindArrivalObject(
        Zone zone,
        ArrivalTargets targets,
        GameObject[] players,
        float zoneCreationDistance,
        bool allowCloseSpawn,
        out GameObject arrivalObject,
        out int arrivalIndex)
    {
        arrivalObject = null!;
        arrivalIndex = 0;
        if (players.Length == 0 || (targets & ArrivalTargets.Objects) == 0)
            return false;

        var maxDistance = Math.Max(
            zoneCreationDistance > 0 ? zoneCreationDistance : DefaultSpawnMaxDistance,
            DefaultSpawnMaxDistance);
        var minDistance = allowCloseSpawn ? 0 : DefaultSpawnMaxDistance;
        var searchDistance = Math.Max(maxDistance * 2.5f, DefaultPersistDistance);
        var bestScore = float.MaxValue;

        foreach (var obj in world.GameWorld.Objects)
        {
            if (string.IsNullOrWhiteSpace(obj.Nickname) ||
                obj.SystemObject == null ||
                !Alive(obj) ||
                !zone.ContainsPoint(obj.WorldTransform.Position) ||
                !obj.TryGetComponent<SDockableComponent>(out var dockable) ||
                dockable.DockPoints.Length == 0 ||
                !dockable.TryGetUndockIndex(out var dockIndex) ||
                !ObjectMatchesArrival(obj, dockable, targets))
            {
                continue;
            }

            var distance = DistanceToNearestPlayer(obj.WorldTransform.Position, players);
            if (distance < minDistance || distance > searchDistance)
                continue;

            var score = distance;
            score += random.NextSingle() * 250f;
            if (score < bestScore)
            {
                bestScore = score;
                arrivalObject = obj;
                arrivalIndex = dockIndex;
            }
        }

        return arrivalObject != null;
    }

    private static bool ObjectMatchesArrival(GameObject obj, SDockableComponent dockable, ArrivalTargets targets)
    {
        if (targets == ArrivalTargets.None)
            return false;

        var isTradelane = dockable.Action.Kind == DockKinds.Tradelane;
        if (isTradelane)
            return (targets & ArrivalTargets.Tradelane) != 0;

        var kinds = GetObjectArrivalTargets(obj, dockable);
        return (targets & kinds) != 0;
    }

    private static ArrivalTargets GetObjectArrivalTargets(GameObject obj, SDockableComponent dockable)
    {
        ArrivalTargets kinds = ArrivalTargets.None;

        var type = obj.SystemObject?.Archetype?.Type ?? ArchetypeType.NONE;
        switch (type)
        {
            case ArchetypeType.docking_ring:
                kinds |= ArrivalTargets.DockingRing;
                break;
            case ArchetypeType.jump_gate:
            case ArchetypeType.jump_hole:
            case ArchetypeType.jumphole:
                kinds |= ArrivalTargets.JumpGate;
                break;
            case ArchetypeType.station:
            case ArchetypeType.weapons_platform:
                kinds |= ArrivalTargets.Station;
                break;
        }

        if (IsCapitalObject(obj))
            kinds |= ArrivalTargets.Capital;

        return kinds;
    }

    private static bool IsCapitalObject(GameObject obj)
    {
        var nickname = obj.SystemObject?.Archetype?.Nickname ?? obj.Nickname ?? string.Empty;
        return nickname.Contains("capital", StringComparison.OrdinalIgnoreCase) ||
               nickname.Contains("battleship", StringComparison.OrdinalIgnoreCase) ||
               nickname.Contains("cruiser", StringComparison.OrdinalIgnoreCase) ||
               nickname.Contains("dreadnought", StringComparison.OrdinalIgnoreCase);
    }
}
