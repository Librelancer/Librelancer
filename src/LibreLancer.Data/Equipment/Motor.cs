// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using LibreLancer.Ini;
namespace LibreLancer.Data.Equipment
{
    public class Motor
    {
        [Entry("nickname")] public string Nickname;
        [Entry("lifetime")] public float Lifetime;
        [Entry("accel")] public float Accel;
        [Entry("delay")] public float Delay;
    }
}