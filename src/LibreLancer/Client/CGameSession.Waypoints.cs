using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.GameData.World;
using LibreLancer.Interface;

namespace LibreLancer.Client;

public partial class CGameSession
{
    private readonly List<Vector3> userWaypoints = [];

    public int UserWaypointCount => userWaypoints.Count;

    public void AddUserWaypoint(Vector3 position) => userWaypoints.Add(position);

    public void ClearUserWaypoints() => userWaypoints.Clear();

    public bool TryGetActiveUserWaypoint(out Vector3 position)
    {
        if (userWaypoints.Count > 0)
        {
            position = userWaypoints[0];
            return true;
        }

        position = default;
        return false;
    }

    public bool RemoveActiveUserWaypoint()
    {
        if (userWaypoints.Count == 0)
            return false;

        userWaypoints.RemoveAt(0);
        return true;
    }

    public void GetUserWaypointsForNavmap(List<NavmapWaypoint> waypoints)
    {
        for (int i = 0; i < userWaypoints.Count; i++)
            waypoints.Add(new NavmapWaypoint(userWaypoints[i], waypoints.Count + 1));
    }

    public string GetUserWaypointPanelText(int index, StarSystem system)
    {
        if (index < 0 || index >= userWaypoints.Count)
            return "";

        var systemName = Game.GameData.GetString(system.IdsName);
        if (string.IsNullOrWhiteSpace(systemName))
            systemName = system.Nickname;

        return $"Player Waypoint {index + 1}\n{systemName} System\nSector {system.WaypointSector(userWaypoints[index])}";
    }
}
