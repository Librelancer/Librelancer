using System;

namespace LibreLancer.Data.GameData.RandomMissions;

[Flags]
public enum AllowedZoneType
{
    None = 0,
    Open = (1 << 0),
    Exclusion = (1 << 2),
    Field = (1 << 3),
    All = Open | Exclusion | Field
}
