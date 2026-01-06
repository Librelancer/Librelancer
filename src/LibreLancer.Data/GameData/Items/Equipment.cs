// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Data.GameData.Items;

public class Equipment : NamedItem
{
    public string? HpType;
    public float[]? LODRanges = [];
    public string? HPChild;
    public ResolvedModel? ModelFile;
    public ResolvedGood? Good;
    public float Volume;

    public LootCrateEquipment? LootAppearance;
    public int UnitsPerContainer;
}
