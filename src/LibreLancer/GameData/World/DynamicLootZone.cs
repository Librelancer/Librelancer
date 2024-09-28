using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.GameData.Items;

namespace LibreLancer.GameData.World;

public class DynamicLootZone : IDataEquatable<DynamicLootZone>
{
    public Zone Zone;
    public LootCrateEquipment LootContainer;
    public Equipment LootCommodity;
    public Vector2 LootCount;
    public float LootDifficulty;


    public DynamicLootZone Clone(Dictionary<string, Zone> newZones)
    {
        var o = (DynamicLootZone)MemberwiseClone();
        o.Zone = Zone == null
            ? null
            : newZones.GetValueOrDefault(Zone.Nickname);
        return o;
    }

    public bool DataEquals(DynamicLootZone other)
    {
        return DataEquality.IdEquals(LootCommodity?.Nickname, other.LootCommodity?.Nickname) &&
               DataEquality.IdEquals(LootContainer?.Nickname, other.LootContainer?.Nickname) &&
               DataEquality.IdEquals(Zone?.Nickname, other.Zone?.Nickname) &&
               LootCount == other.LootCount &&
               // ReSharper disable once CompareOfFloatsByEqualityOperator
               LootDifficulty == other.LootDifficulty;
    }
}
