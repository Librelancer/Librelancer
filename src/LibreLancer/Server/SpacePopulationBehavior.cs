using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Missions.Directives;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;
using Zone = LibreLancer.Data.GameData.World.Zone;

namespace LibreLancer.Server;

public partial class SpacePopulationManager
{
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
}
