// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Equipment;

[ParsedSection]
public partial class Motor
{
    [Entry("nickname", Required = true)] public string Nickname = null!;
    [Entry("lifetime")] public float Lifetime;
    [Entry("accel")] public float Accel;
    [Entry("delay")] public float Delay;
}
