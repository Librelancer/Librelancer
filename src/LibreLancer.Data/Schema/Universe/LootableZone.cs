// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe;

[ParsedSection]
public partial class LootableZone
{
    [Entry("zone")] public string? Zone; // Refers to whole field when null?
    [Entry("asteroid_loot_container")] public string? AsteroidLootContainer;
    [Entry("asteroid_loot_commodity")] public string? AsteroidLootCommodity;
    [Entry("dynamic_loot_container")] public string? DynamicLootContainer;
    [Entry("dynamic_loot_commodity")] public string? DynamicLootCommodity;
    [Entry("asteroid_loot_count")] public Vector2 AsteroidLootCount;
    [Entry("dynamic_loot_count")] public Vector2 DynamicLootCount;
    [Entry("asteroid_loot_difficulty")] public float AsteroidLootDifficulty;
    [Entry("dynamic_loot_difficulty")] public float DynamicLootDifficulty;
}
