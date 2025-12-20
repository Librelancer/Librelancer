using System;

namespace LibreLancer.Data.GameData.World;

[Flags]
public enum VisitFlags
{
    None = 0,
    Visited = 1,
    Unused = 2,
    MineableZone = 4,
    ActivelyVisited = 8, //Looted wreck?
    Wreck = 16,
    Zone = 32,
    Faction = 64,
    Hidden = 128
}