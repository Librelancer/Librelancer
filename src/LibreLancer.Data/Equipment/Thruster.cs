// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Data.Equipment
{
	public class Thruster : AbstractEquipment
	{
        [Entry("particles")]
		public string Particles;
        [Entry("da_archetype")]
		public string DaArchetype;
        [Entry("material_library")]
		public string MaterialLibrary;
        [Entry("hp_particles")]
		public string HpParticles;
        [Entry("max_force")]
		public int MaxForce;
        [Entry("power_usage")]
		public int PowerUsage;
        [Entry("ids_name")]
		public int IdsName;
        [Entry("ids_info")]
		public int IdsInfo;
        [Entry("hit_pts")]
		public int Hitpoints;
	}
}
