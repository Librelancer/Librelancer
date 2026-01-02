// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Equipment;

[ParsedSection]
public partial class Thruster : AbstractEquipment
{
    [Entry("particles")]
    public string? Particles;
    [Entry("hp_particles")]
    public string? HpParticles;
    [Entry("max_force")]
    public int MaxForce;
    [Entry("power_usage")]
    public int PowerUsage;
}
