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
}
