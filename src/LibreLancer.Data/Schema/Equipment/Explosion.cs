// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Equipment;

[ParsedSection]
public partial class Explosion
{
    [Entry("nickname", Required = true)] public string Nickname = null!;
    [Entry("effect")] public string? Effect;
    [Entry("lifetime")] public Vector2 Lifetime;
    [Entry("process")] public string? Process;
    [Entry("strength")] public float Strength;
    [Entry("radius")] public float Radius;
    [Entry("hull_damage")] public float HullDamage;
    [Entry("energy_damage")] public float EnergyDamage;
    [Entry("impulse")] public float Impulse;
}
