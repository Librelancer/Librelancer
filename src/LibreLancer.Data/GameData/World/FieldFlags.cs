using System;
using System.Collections.Generic;

namespace LibreLancer.Data.GameData.World;

[Flags]
public enum FieldFlags
{
    ObjectDensityLow = (1 << 0),
    ObjectDensityMed = (1 << 1),
    ObjectDensityHigh = (1 << 2),
    DangerDensityLow = (1 << 3),
    DangerDensityMed = (1 << 4),
    DangerDensityHigh = (1 << 5),
    RockObjects = (1 << 6),
    LavaObjects = (1 << 7),
    NomadObjects = (1 << 8),
    IceObjects = (1 << 9),
    GasDangerObjects = (1 << 10),
    CrystalObjects = (1 << 11),
    BadlandDangerObjects = (1 << 12),
    DebrisObjects = (1 << 13),
    MineDangerObjects = (1 << 14),
}

public static class FieldFlagUtils
{
    private static Dictionary<string, FieldFlags> stringToFlag =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "object_density_low", FieldFlags.ObjectDensityLow },
            { "object_density_med", FieldFlags.ObjectDensityMed },
            { "object_density_medium", FieldFlags.ObjectDensityMed },
            { "object_density_high", FieldFlags.ObjectDensityHigh },
            { "danger_density_low", FieldFlags.DangerDensityLow },
            { "danger_density_med", FieldFlags.DangerDensityMed },
            { "danger_density_medium", FieldFlags.DangerDensityMed },
            { "DANGER_DENSITY_HIGH", FieldFlags.DangerDensityHigh },
            { "rock_objects", FieldFlags.RockObjects },
            { "nomad_objects", FieldFlags.NomadObjects },
            { "lava_objects", FieldFlags.LavaObjects },
            { "ice_objects", FieldFlags.IceObjects },
            { "gas_danger_objects", FieldFlags.GasDangerObjects },
            { "crystal_objects", FieldFlags.CrystalObjects },
            { "badland_danger_objects", FieldFlags.BadlandDangerObjects },
            { "debris_objects", FieldFlags.DebrisObjects },
            { "MINE_DANGER_OBJECTS", FieldFlags.MineDangerObjects }
        };

    private static readonly string[] flagToString =
    [
        "object_density_low",
        "object_density_med",
        "object_density_high",
        "danger_density_low",
        "danger_density_med",
        "DANGER_DENSITY_HIGH",
        "rock_objects",
        "nomad_objects",
        "lava_objects",
        "ice_objects",
        "gas_danger_objects",
        "crystal_objects",
        "badland_danger_objects",
        "debris_objects",
        "MINE_DANGER_OBJECTS"
    ];

    public static bool TryParse(string s, out FieldFlags result) => stringToFlag.TryGetValue(s, out result);

    public static IEnumerable<string> GetStringValues(this FieldFlags flags)
    {
        for (int i = 0; i < flagToString.Length; i++)
        {
            if ((flags & (FieldFlags)(1 << i)) == (FieldFlags)(1 << i))
            {
                yield return flagToString[i];
            }
        }
    }

}
