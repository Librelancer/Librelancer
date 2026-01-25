// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;
using static LibreLancer.Data.Schema.Equipment.HpCategory;
namespace LibreLancer.Data.Schema.Equipment;

public class HpTypesIni
{
    private static HpType[] defaults =
    [
        new("hp_freighter_shield_special_10", External, 10, 1730, 914),
        new("hp_freighter_shield_special_9", External, 9, 1729, 914),
        new("hp_freighter_shield_special_8", External, 8, 1728, 914),
        new("hp_freighter_shield_special_7", External, 7, 1727, 914),
        new("hp_freighter_shield_special_6", External, 6, 1726, 914),
        new("hp_freighter_shield_special_5", External, 5, 1725, 914),
        new("hp_freighter_shield_special_4", External, 4, 1724, 914),
        new("hp_freighter_shield_special_3", External, 3, 1723, 914),
        new("hp_freighter_shield_special_2", External, 2, 1722, 914),
        new("hp_freighter_shield_special_1", External, 1, 1721, 914),
        new("hp_fighter_shield_special_10", External, 10, 1710, 912),
        new("hp_fighter_shield_special_9", External, 9, 1709, 912),
        new("hp_fighter_shield_special_8", External, 8, 1708, 912),
        new("hp_fighter_shield_special_7", External, 7, 1707, 912),
        new("hp_fighter_shield_special_6", External, 6, 1706, 912),
        new("hp_fighter_shield_special_5", External, 5, 1705, 912),
        new("hp_fighter_shield_special_4", External, 4, 1704, 912),
        new("hp_fighter_shield_special_3", External, 3, 1702, 912),
        new("hp_fighter_shield_special_2", External, 2, 1701, 912),
        new("hp_fighter_shield_special_1", External, 1, 1700, 912),
        new("hp_elite_shield_special_10", External, 10, 1720, 913),
        new("hp_elite_shield_special_9", External, 9, 1719, 913),
        new("hp_elite_shield_special_8", External, 8, 1718, 913),
        new("hp_elite_shield_special_7", External, 7, 1717, 913),
        new("hp_elite_shield_special_6", External, 6, 1716, 913),
        new("hp_elite_shield_special_5", External, 5, 1715, 913),
        new("hp_elite_shield_special_4", External, 4, 1714, 913),
        new("hp_elite_shield_special_3", External, 3, 1713, 913),
        new("hp_elite_shield_special_2", External, 2, 1712, 913),
        new("hp_elite_shield_special_1", External, 1, 1711, 913),
        new("hp_fighter_shield_generator", External, 0, 1517, 912),
        new("hp_elite_shield_generator", External, 0, 1518, 913),
        new("hp_freighter_shield_generator", External, 0, 1519, 914),
        new("hp_thruster", External, 0, 1520, 915),
        new("hp_gun_special_1", Weapon, 1, 1525, 907),
        new("hp_gun_special_2", Weapon, 2, 1526, 907),
        new("hp_gun_special_3", Weapon, 3, 1527, 907),
        new("hp_gun_special_4", Weapon, 4, 1528, 907),
        new("hp_gun_special_5", Weapon, 5, 1529, 907),
        new("hp_gun_special_6", Weapon, 6, 1530, 907),
        new("hp_gun_special_7", Weapon,7, 1531, 907),
        new("hp_gun_special_8", Weapon, 8, 1532, 907),
        new("hp_gun_special_9", Weapon, 9, 1533, 907),
        new("hp_gun_special_10", Weapon, 10, 1534, 907),
        /* CD = class 1, Torp = class 2, ensures display is correct */
        new("hp_torpedo_special_2", Weapon, 1, 1742, 916),
        new("hp_torpedo_special_1", Weapon, 2, 1741, 908),
        new("hp_turret_special_1", Weapon, 1, 1731, 909),
        new("hp_turret_special_2", Weapon, 2, 1732, 909),
        new("hp_turret_special_3", Weapon, 3, 1733, 909),
        new("hp_turret_special_4", Weapon, 4, 1734, 909),
        new("hp_turret_special_5", Weapon, 5, 1735, 909),
        new("hp_turret_special_6", Weapon, 6, 1736, 909),
        new("hp_turret_special_7", Weapon, 7, 1737, 909),
        new("hp_turret_special_8", Weapon, 8, 1738, 909),
        new("hp_turret_special_9", Weapon, 9, 1739, 909),
        new("hp_turret_special_10", Weapon, 10, 1740, 909),
        new("hp_mine_dropper", Weapon, 0, 1522, 911),
        /*new HpType("hp_countermeasure", Weapon, 0, 1523, 910),*/
        new("hp_countermeasure_dropper", Weapon, 0, 1523, 910)
    ];

    public Dictionary<string, HpType> Types = new(StringComparer.OrdinalIgnoreCase);

    private void AddType(HpType type)
    {
        type.SortIndex = Types.Count;
        Types.Add(type.Type, type);
    }

    public void LoadDefault()
    {
        foreach (var t in defaults)
            AddType(t);
    }

    public void LoadFromIni(string path, FileSystem vfs)
    {
        foreach (var section in IniFile.ParseFile(path, vfs))
        {
            if (section.Name.ToLower() != "hardpoints")
            {
                continue;
            }

            foreach (var x in section)
            {
                if (x.Count == 4)
                {
                    AddType(new HpType(
                        x.Name,
                        Enum.Parse<HpCategory>(x[0].ToString(), true),
                        x[1].ToInt32(),
                        x[2].ToInt32(),
                        x[3].ToInt32()
                    ));
                }
                else
                {
                    FLLog.Error("Ini", $"Invalid entry in hardpoint types '{x.Name}'");
                }
            }
        }
    }
}

public enum HpCategory
{
    Weapon,
    External,
    Internal
}

public struct HpType
{
    public string Type;
    public int Class;
    public int IdsName;
    public int IdsHpDescription;
    public HpCategory Category;
    public int SortIndex;
    public HpType(string type, HpCategory category, int cls, int idsname, int idshpdescription)
    {
        Type = type;
        Category = category;
        Class = cls;
        IdsName = idsname;
        IdsHpDescription = idshpdescription;
        SortIndex = 0;
    }
}
