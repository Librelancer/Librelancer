using System.Numerics;

namespace LibreLancer.Client;

public enum UserWaypointKind
{
    ManualDestination,
    JumpEntry,
    TradelaneEntry,
    TradelaneExit
}

public readonly record struct UserWaypoint(
    uint SystemHash,
    Vector3 Position,
    UserWaypointKind Kind,
    uint? TargetObjectHash = null
);
