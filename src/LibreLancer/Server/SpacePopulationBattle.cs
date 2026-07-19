using System;
using System.Linq;
using System.Numerics;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server;

public partial class SpacePopulationManager
{
    private void UpdateBattleState(ZoneState state, GameObject[] players, double delta)
    {
        if (players.Length > 0 && state.Groups.Any(x => GroupInCombat(x, players)))
        {
            state.BattleCooldown = BattleCooldownSeconds;
            return;
        }

        state.BattleCooldown = Math.Max(0, state.BattleCooldown - delta);
    }

    private void UpdateGroupCombat(ZoneState state)
    {
        foreach (var group in state.Groups)
        {
            var leader = group.Ships.FirstOrDefault(Alive);
            if (leader == null)
                continue;

            var hostile = FindGroupHostile(group, group.InCombat ? BattleDistance : CombatEngageDistance);
            if (hostile != null)
            {
                if (!group.InCombat)
                {
                    group.ResumeDutyTarget = GetDutyResumeTarget(group, leader.WorldTransform.Position);
                    SuspendGroupDirectives(group);
                }
                group.InCombat = true;
                state.BattleCooldown = BattleCooldownSeconds;
                continue;
            }

            if (!group.InCombat)
                continue;

            group.InCombat = false;
            group.InitialPathTarget = group.ResumeDutyTarget;
            group.ResumeDutyTarget = null;
            ClearCombatTargets(group);
            AssignDirectives(group);
        }
    }

    private GameObject? FindGroupHostile(PopGroup group, float range)
    {
        foreach (var ship in group.Ships)
        {
            if (!Alive(ship))
                continue;

            var hostile = FindNearbyHostile(ship, range);
            if (hostile != null)
                return hostile;
        }

        return null;
    }

    private GameObject? FindNearbyHostile(GameObject ship, float range)
    {
        if (!ship.TryGetComponent<SRepComponent>(out var rep))
            return null;

        var rangeSquared = range * range;
        var position = ship.WorldTransform.Position;
        GameObject? selected = null;
        var selectedDistance = float.MaxValue;

        foreach (var other in world.GameWorld.SpatialLookup.GetNearbyObjects(ship, position, range))
        {
            if (!Alive(other) ||
                ReferenceEquals(other, ship) ||
                (other.Flags & GameObjectFlags.Cloaked) == GameObjectFlags.Cloaked ||
                other.TryGetComponent<STradelaneMoveComponent>(out _) ||
                !rep.IsHostileTo(other))
            {
                continue;
            }

            var distance = Vector3.DistanceSquared(position, other.WorldTransform.Position);
            if (distance > rangeSquared || distance >= selectedDistance)
                continue;

            selected = other;
            selectedDistance = distance;
        }

        return selected;
    }

    private Vector3? GetDutyResumeTarget(PopGroup group, Vector3 position)
    {
        var path = group.State.Path;
        if (path == null || group.PathIndex < 0)
            return null;

        var index = Math.Clamp(group.PathIndex, 0, path.Count - 1);
        if (!TryGetPatrolPathLine(path, index, out var start, out var end))
            return path[index].Zone.Position;

        return ClosestPointOnSegment(start, end, position);
    }

    private static void SuspendGroupDirectives(PopGroup group)
    {
        foreach (var ship in group.Ships)
        {
            if (!Alive(ship))
                continue;

            ship.GetComponent<DirectiveRunnerComponent>()?.Cancel();
        }
    }

    private static void ClearCombatTargets(PopGroup group)
    {
        foreach (var ship in group.Ships)
        {
            if (!Alive(ship))
                continue;

            if (ship.TryGetComponent<SNPCComponent>(out var npc))
                npc.CurrentDirective = null;
            if (ship.TryGetComponent<SelectedTargetComponent>(out var selected))
                selected.Selected = null;
        }
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

                if (group.Ships.Any(x => IsInsideRandomMissionNoSpawnZone(x.WorldTransform.Position)))
                {
                    foreach (var ship in group.Ships)
                    {
                        if (Alive(ship))
                            world.RemoveSpawnedObject(ship, false);
                    }
                    state.Groups.RemoveAt(i);
                    continue;
                }

                var basePersistDistance = group.PersistDistance > 0
                    ? group.PersistDistance
                    : DefaultPersistDistance;
                var persistDistance = state.InBattle || GroupInCombat(group, players)
                    ? Math.Max(basePersistDistance, BattlePersistDistance)
                    : basePersistDistance;
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
}
