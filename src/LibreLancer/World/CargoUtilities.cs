using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Server;

namespace LibreLancer.World;

public static class CargoUtilities
{
    public static float GetUsedVolume(List<NetCargo> items) => items.Select(x => x.Count * x.Equipment.Volume).Sum();

    public static int ItemCount(List<NetCargo> items, Equipment e) =>
        items.Where(x => x.Equipment == e).Sum(x => x.Count);

    public static int ItemCount<T>(List<NetCargo> items) where T : Equipment =>
        items.Where(x => x.Equipment is T).Sum(x => x.Count);
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
