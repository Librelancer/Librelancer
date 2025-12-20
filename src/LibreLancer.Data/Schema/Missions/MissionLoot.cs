// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Missions
{
    [ParsedSection]
    public partial class MissionLoot
    {
        [Entry("nickname")]
        public string Nickname;
        [Entry("archetype")]
        public string Archetype;
        [Entry("string_id")]
        public int StringId;
        [Entry("position")]
        public Vector3 Position;
        [Entry("rel_pos_obj")]
        public string RelPosObj;
        [Entry("rel_pos_offset")]
        public Vector3 RelPosOffset;
        [Entry("velocity")]
        public Vector3 Velocity;
        [Entry("equip_amount")]
        public int EquipAmount;
        [Entry("health")]
        public float Health;
        [Entry("can_jettison")]
        public bool CanJettison;
    }
}
