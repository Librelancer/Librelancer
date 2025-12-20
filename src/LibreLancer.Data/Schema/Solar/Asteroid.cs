// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Solar
{
    [ParsedSection]
	public partial class Asteroid
	{
        [Entry("nickname", Required = true)]
		public string Nickname;
        [Entry("DA_archetype")]
		public string DaArchetype;
        [Entry("material_library")]
		public string MaterialLibrary;
        [Entry("explosion_arch")]
        public string ExplosionArch;
        [Entry("detect_radius")]
        public int DetectRadius;
        [Entry("explosion_offset")]
        public int ExplosionOffset;
        [Entry("recharge_time")]
        public float RechargeTime;

		public bool IsMine;
	}
}

