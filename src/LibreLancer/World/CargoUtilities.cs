using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.Schema.Equipment;
using LibreLancer.Server;

namespace LibreLancer.World;

public static class CargoUtilities
{
    public static float GetUsedVolume(List<NetCargo> items) => items.Select(x => x.Count * x.Equipment!.Volume).Sum();

    public static int ItemCount(List<NetCargo> items, Equipment e) =>
        items.Where(x => x.Equipment == e).Sum(x => x.Count);

    public static int ItemCount<T>(List<NetCargo> items) where T : Equipment =>
        items.Where(x => x.Equipment is T).Sum(x => x.Count);

    public static IEnumerable<string> CompatibleHardpoints(Ship ship, HpTypesIni hpTypes, string? hpType)
    {
        HashSet<string> hardpoints = new(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(hpType))
            return hardpoints;
        if (ship.PossibleHardpoints.TryGetValue(hpType, out var possible))
            hardpoints.UnionWith(possible);
        if (!hpTypes.Types.TryGetValue(hpType, out var equipmentType))
            return hardpoints;
        var family = HpFamily(equipmentType.Type);
        foreach (var hp in ship.HardpointTypes)
        {
            if (hp.Value.Any(shipType => shipType.Category == equipmentType.Category &&
                                         shipType.Class >= equipmentType.Class &&
                                         HpFamily(shipType.Type).Equals(family, StringComparison.OrdinalIgnoreCase)))
                hardpoints.Add(hp.Key);
        }
        return hardpoints;
    }

    public static bool HasCompatibleHardpoint(Ship ship, HpTypesIni hpTypes, string? hpType) =>
        CompatibleHardpoints(ship, hpTypes, hpType).Any();

    public static bool SupportsHardpoint(Ship ship, HpTypesIni hpTypes, string? hpType, string? hardpoint) =>
        CompatibleHardpoints(ship, hpTypes, hpType).Any(x => x.Equals(hardpoint, StringComparison.OrdinalIgnoreCase));

    private static string HpFamily(string type)
    {
        var index = type.Length - 1;
        while (index >= 0 && char.IsDigit(type[index]))
            index--;
        return index < type.Length - 1 && index >= 0 && type[index] == '_' ? type[..index] : type;
    }

    public static int GetItemLimit(List<NetCargo> items, Ship ship, Equipment equipment)
    {
        var maxAmount = int.MaxValue;
        if (equipment.Volume > 0)
        {
            var freeSpace = ship.HoldSize - GetUsedVolume(items);
            if (freeSpace < 0) freeSpace = 0;
            var maxStorage = (int)Math.Floor(freeSpace / equipment.Volume);
            if (maxStorage < maxAmount)
                maxAmount = maxStorage;
        }
        if (equipment is RepairKitEquipment)
        {
            if (ship.MaxRepairKits < maxAmount)
                maxAmount = (ship.MaxRepairKits - ItemCount<RepairKitEquipment>(items));
        }
        else if (equipment is ShieldBatteryEquipment)
        {
            if (ship.MaxShieldBatteries < maxAmount)
                maxAmount = (ship.MaxShieldBatteries - ItemCount<ShieldBatteryEquipment>(items));
        }
        else if (equipment is MissileEquip)
        {
            if (maxAmount > 50)
                maxAmount = (50 - ItemCount(items, equipment));
        }
        return maxAmount < 0 ? 0 : maxAmount;
    }
}
