// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

using LibreLancer.Ini;

namespace LibreLancer.Compatibility.GameData.Equipment
{
    public abstract class AbstractEquipment
    {
        [Entry("nickname")]
        public string Nickname;
        [Entry("lodranges")]
        public float[] LODRanges;
        [Entry("hp_child")]
        public string HPChild { get; private set; }
    }
}
