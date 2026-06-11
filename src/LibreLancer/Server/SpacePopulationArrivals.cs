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

public partial class SpacePopulationManager
{
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
}
