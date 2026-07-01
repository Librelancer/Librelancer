using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.GameData.World;
using LibreLancer.Interface;

namespace LibreLancer.Client;

public partial class CGameSession
{
    private readonly List<UserWaypoint> userWaypoints = [];
    private uint? pendingRouteDockHash;
    private bool bestPathActive;
    private uint bestPathDestinationSystem;
    private Vector3 bestPathDestinationPosition;

    public int UserWaypointCount => userWaypoints.Count;
    public bool BestPathActive => bestPathActive;

    public void AddUserWaypoint(StarSystem system, Vector3 position)
    {
        bestPathActive = false;
        userWaypoints.Add(new UserWaypoint(system.CRC, position, UserWaypointKind.ManualDestination));
    }

    public void ReplaceUserWaypoints(IEnumerable<UserWaypoint> waypoints)
    {
        ReplaceUserWaypoints(waypoints, false);
    }

    private void ReplaceUserWaypoints(IEnumerable<UserWaypoint> waypoints, bool preserveBestPathState)
    {
        userWaypoints.Clear();
        userWaypoints.AddRange(waypoints);
        pendingRouteDockHash = null;
        if (!preserveBestPathState)
            bestPathActive = false;
    }

    public void ClearUserWaypoints()
    {
        userWaypoints.Clear();
        pendingRouteDockHash = null;
        bestPathActive = false;
    }

    public bool TryGetActiveUserWaypoint(uint currentSystemHash, out UserWaypoint waypoint)
    {
        if (userWaypoints.Count > 0 && userWaypoints[0].SystemHash == currentSystemHash)
        {
            waypoint = userWaypoints[0];
            return true;
        }

        waypoint = default;
        return false;
    }

    public bool RemoveActiveUserWaypoint()
    {
        if (userWaypoints.Count == 0)
            return false;

        userWaypoints.RemoveAt(0);
        if (userWaypoints.Count == 0)
        {
            pendingRouteDockHash = null;
            bestPathActive = false;
        }
        return true;
    }

    public void GetUserWaypointsForNavmap(StarSystem displayedSystem, List<NavmapWaypoint> waypoints)
    {
        for (int i = 0; i < userWaypoints.Count; i++)
        {
            if (userWaypoints[i].SystemHash != displayedSystem.CRC)
                continue;
            waypoints.Add(new NavmapWaypoint(userWaypoints[i].Position, i + 1));
        }
    }

    public string GetUserWaypointPanelText(int index)
    {
        if (index < 0 || index >= userWaypoints.Count)
            return "";

        var waypoint = userWaypoints[index];
        var system = Game.GameData.Items.Systems.Get(waypoint.SystemHash);
        if (system == null)
            return "";

        var systemName = Game.GameData.GetString(system.IdsName);
        if (string.IsNullOrWhiteSpace(systemName))
            systemName = system.Nickname;

        return $"Player Waypoint {index + 1}\n{KindLabel(waypoint.Kind)}\n{systemName} System\nSector {system.WaypointSector(waypoint.Position)}";
    }

    public void RegisterRouteDock(uint targetObjectHash, uint currentSystemHash)
    {
        pendingRouteDockHash = null;
        if (!TryGetActiveUserWaypoint(currentSystemHash, out var waypoint))
            return;
        if (waypoint.Kind is not (UserWaypointKind.JumpEntry or UserWaypointKind.TradelaneEntry))
            return;
        if (waypoint.TargetObjectHash != targetObjectHash)
            return;
        pendingRouteDockHash = targetObjectHash;
    }

    public bool CompleteActiveJumpWaypoint(uint previousSystemHash, uint newSystemHash)
    {
        if (!TryGetActiveUserWaypoint(previousSystemHash, out var waypoint))
            return false;
        if (waypoint.Kind != UserWaypointKind.JumpEntry ||
            waypoint.TargetObjectHash == null ||
            pendingRouteDockHash != waypoint.TargetObjectHash ||
            previousSystemHash == newSystemHash)
            return false;

        pendingRouteDockHash = null;
        return RemoveActiveUserWaypoint();
    }

    public bool CompleteActiveTradelaneWaypoint(uint currentSystemHash)
    {
        if (!TryGetActiveUserWaypoint(currentSystemHash, out var waypoint))
            return false;
        if (waypoint.Kind != UserWaypointKind.TradelaneEntry ||
            waypoint.TargetObjectHash == null ||
            pendingRouteDockHash != waypoint.TargetObjectHash)
            return false;

        pendingRouteDockHash = null;
        return RemoveActiveUserWaypoint();
    }

    public bool ComputeBestPathToSelection(
        StarSystem currentSystem,
        Vector3 currentPosition,
        StarSystem destinationSystem,
        Vector3 destinationPosition,
        float cruiseSpeed)
    {
        var route = ComputeBestPath(currentSystem, currentPosition, destinationSystem, destinationPosition, cruiseSpeed);
        if (route.Count == 0)
            return false;

        var changed = !WaypointsEqual(route);
        bestPathActive = true;
        bestPathDestinationSystem = destinationSystem.CRC;
        bestPathDestinationPosition = destinationPosition;
        if (changed)
            ReplaceUserWaypoints(route, true);
        return true;
    }

    public bool RecalculateBestPath(StarSystem currentSystem, Vector3 currentPosition, float cruiseSpeed)
    {
        if (!bestPathActive)
            return false;
        var destinationSystem = Game.GameData.Items.Systems.Get(bestPathDestinationSystem);
        if (destinationSystem == null)
        {
            bestPathActive = false;
            return false;
        }

        var route = ComputeBestPath(
            currentSystem,
            currentPosition,
            destinationSystem,
            bestPathDestinationPosition,
            cruiseSpeed);
        if (route.Count == 0)
            return false;

        if (WaypointsEqual(route))
            return false;

        ReplaceUserWaypoints(route, true);
        return true;
    }

    private List<UserWaypoint> ComputeBestPath(
        StarSystem currentSystem,
        Vector3 currentPosition,
        StarSystem destinationSystem,
        Vector3 destinationPosition,
        float cruiseSpeed) =>
        NavmapBestPathPlanner.Compute(
            Game.GameData.Items.Systems,
            currentSystem,
            currentPosition,
            destinationSystem,
            destinationPosition,
            IsVisited,
            cruiseSpeed);

    private bool WaypointsEqual(List<UserWaypoint> route)
    {
        if (userWaypoints.Count != route.Count)
            return false;
        for (var i = 0; i < route.Count; i++)
        {
            if (userWaypoints[i] != route[i])
                return false;
        }
        return true;
    }

    private static string KindLabel(UserWaypointKind kind) => kind switch
    {
        UserWaypointKind.JumpEntry => "Jump Route",
        UserWaypointKind.TradelaneEntry => "Tradelane Route",
        UserWaypointKind.TradelaneExit => "Tradelane Exit",
        _ => "Manual Destination"
    };
}
