// MIT License - Copyright (c) Lazrius
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Entities.Enums
{
    // Source: https://the-starport.net/freelancer/forum/viewtopic.php?post_id=34251#forumpost34251
    public enum Visit
    {
        // default value, not visited, nothing special
        None = 0,
        //visited, will show on Nav Map/rep list
        Visited = 1,
        //  unknown, doesn't seem used
        Unknown = 2,
        MineableZone = 4,
        // "actively" visited (looted wreck is the only real meaning)
        ActivelyVisited = 8,
        // Wreck
        Wreck = 16,
        // Zone
        Zone = 32,
        // Faction
        Faction = 64,
        // hidden, never shows on Nav Map
        Hidden = 128
    }
}