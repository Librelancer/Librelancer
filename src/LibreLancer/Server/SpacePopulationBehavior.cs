using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Missions;
using LibreLancer.Missions.Directives;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;
using Zone = LibreLancer.Data.GameData.World.Zone;

namespace LibreLancer.Server;

public partial class SpacePopulationManager
{
    private const float PatrolPathWaypointRange = 250f;
    private const int MaxPatrolPathTargets = 4;

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
        var retirementDockable = GetRetirementDockable(group, currentPosition);
        if (retirementDockable != null)
            return BuildRetirementDirectives(retirementDockable);

        var behavior = group.Encounter.FormationDefinition?.Behavior ?? EncounterBehavior.wander;
        return behavior switch
        {
            EncounterBehavior.patrol_path => BuildPathDirectives(group, GotoKind.GotoCruise, 100, PatrolPathWaypointRange),
            EncounterBehavior.trade when IsPatrol(group.State.Zone) => BuildPathDirectives(group, GotoKind.GotoCruise, 100, PatrolPathWaypointRange),
            EncounterBehavior.trade => BuildTradeDirectives(group.State, currentPosition, group.ArrivalObject),
            _ => BuildWanderDirectives(group.State.Zone)
        };
    }

    private GameObject? GetRetirementDockable(PopGroup group, Vector3 currentPosition)
    {
        var reliefTime = group.State.Zone.ReliefTime;
        if (reliefTime <= 0 || group.AgeSeconds < reliefTime)
            return null;

        var behavior = group.Encounter.FormationDefinition?.Behavior ?? EncounterBehavior.wander;
        if (behavior != EncounterBehavior.patrol_path && !IsPatrol(group.State.Zone))
            return null;

        if (HasNextPatrolPathSegment(group))
            return null;

        return FindRetirementDockable(group, currentPosition);
    }

    private static bool HasNextPatrolPathSegment(PopGroup group)
    {
        var path = group.State.Path;
        if (path == null || path.Count < 2 || group.PathIndex < 0)
            return false;

        return IsClosedPath(path) || group.PathIndex + 1 < path.Count;
    }

    private static MissionDirective[] BuildRetirementDirectives(GameObject dockable)
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

        return BuildPathDirectives(state, GotoKind.GotoCruise, 100);
    }

    private MissionDirective[] BuildPathDirectives(
        ZoneState state,
        GotoKind kind,
        float throttle,
        float range = 750)
    {
        var targets = GetPathTargets(state);
        if (targets.Count == 0)
            targets.Add(SampleZonePoint(state.Zone));

        return BuildGotoDirectives(targets, kind, throttle, range);
    }

    private MissionDirective[] BuildPathDirectives(
        PopGroup group,
        GotoKind kind,
        float throttle,
        float range = 750)
    {
        var targets = GetPathTargets(group);
        if (targets.Count == 0)
            targets.Add(SampleZonePoint(group.State.Zone));

        return BuildGotoDirectives(targets, kind, throttle, range);
    }

    private static MissionDirective[] BuildGotoDirectives(
        List<Vector3> targets,
        GotoKind kind,
        float throttle,
        float range)
    {
        return targets.Select(x => (MissionDirective)new GotoVecDirective
        {
            Target = x,
            CruiseKind = kind,
            Range = range,
            MaxThrottle = throttle
        }).ToArray();
    }

    private List<Vector3> GetPathTargets(ZoneState state)
    {
        var result = new List<Vector3>();
        if (state.Path == null || state.PathIndex < 0)
            return result;

        for (int i = state.PathIndex; i < state.Path.Count && result.Count < MaxPatrolPathTargets; i++)
            AddPatrolSegmentTargets(result, state.Path, i, i != state.PathIndex);

        return result;
    }

    private List<Vector3> GetPathTargets(PopGroup group)
    {
        var result = new List<Vector3>();
        var path = group.State.Path;
        if (path == null || path.Count == 0 || group.PathIndex < 0)
            return result;

        if (group.InitialPathTarget is { } initialTarget)
        {
            result.Add(initialTarget);
            group.InitialPathTarget = null;
        }

        var index = Math.Clamp(group.PathIndex, 0, path.Count - 1);
        var includeCurrent = result.Count > 0 &&
                             TryGetPatrolPathLine(path, index, out _, out _);
        var closedLoop = IsClosedPath(path);

        for (int count = 0; count < MaxPatrolPathTargets && result.Count < MaxPatrolPathTargets && path.Count > 1; count++)
        {
            if (!includeCurrent)
            {
                index++;
                if (closedLoop)
                    index %= path.Count;
                else if (index >= path.Count)
                    break;
            }

            AddPatrolSegmentTargets(result, path, index, index != group.PathIndex);
            includeCurrent = false;
        }

        if (result.Count == 0)
        {
            result.Add(SampleZonePoint(group.State.Zone));
        }

        group.PathIndex = index;
        return result;
    }

    private static void AddPatrolSegmentTargets(
        List<Vector3> result,
        List<PatrolPathSegment> path,
        int pathIndex,
        bool includeStart)
    {
        if (!TryGetPatrolPathLine(path, pathIndex, out var start, out var end))
        {
            AddPatrolTarget(result, path[pathIndex].Zone.Position);
            return;
        }

        if (includeStart && result.Count + 1 < MaxPatrolPathTargets)
            AddPatrolTarget(result, start);
        AddPatrolTarget(result, end);
    }

    private static void AddPatrolTarget(List<Vector3> result, Vector3 target)
    {
        if (result.Count >= MaxPatrolPathTargets)
            return;
        if (result.Count > 0 &&
            Vector3.DistanceSquared(result[^1], target) < PatrolPathWaypointRange * PatrolPathWaypointRange)
        {
            return;
        }
        result.Add(target);
    }

    private static bool IsClosedPath(List<PatrolPathSegment> path)
    {
        if (path.Count < 3)
            return false;

        if (ReferenceEquals(path[0].Zone, path[^1].Zone))
            return true;

        var first = path[0].Zone.Position;
        var last = path[^1].Zone.Position;
        return Vector3.DistanceSquared(first, last) < 1;
    }

    private GameObject? FindRetirementDockable(PopGroup group, Vector3 currentPosition)
    {
        var maxDistance = group.PersistDistance > 0
            ? group.PersistDistance
            : DefaultPersistDistance;
        var maxDistanceSquared = maxDistance * maxDistance;

        GameObject? nearest = null;
        var nearestDistance = float.MaxValue;
        foreach (var obj in world.GameWorld.Objects)
        {
            if (string.IsNullOrWhiteSpace(obj.Nickname) ||
                obj.SystemObject == null ||
                !Alive(obj) ||
                !obj.TryGetComponent<SDockableComponent>(out var dockable) ||
                dockable.Action.Kind != DockKinds.Base ||
                dockable.DockPoints.Length == 0)
            {
                continue;
            }

            var distance = Vector3.DistanceSquared(currentPosition, obj.WorldTransform.Position);
            if (distance > maxDistanceSquared || distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            nearest = obj;
        }
        return nearest;
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

    private Vector3 GetFirstDirectiveTarget(ZoneState state, EncounterInfo info)
    {
        var behavior = info.FormationDefinition?.Behavior ?? EncounterBehavior.wander;
        if (behavior == EncounterBehavior.patrol_path || IsPatrol(state.Zone))
        {
            var targets = GetPathTargets(state);
            if (targets.Count > 0)
                return targets[0];
        }
        return SampleZonePoint(state.Zone);
    }

    private void UpdateIdleGroupDirectives(ZoneState state)
    {
        foreach (var group in state.Groups)
        {
            if (group.InCombat)
                continue;

            var leader = group.Ships.FirstOrDefault(Alive);
            if (leader == null)
                continue;
            var runner = leader.GetComponent<DirectiveRunnerComponent>();
            if (runner is { Active: false })
                AssignDirectives(group);
        }
    }
}
