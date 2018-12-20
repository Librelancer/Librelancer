// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Data.Equipment
{
	public class PowerCore : AbstractEquipment
	{
        [Entry("da_archetype")]
		public string DaArchetype;
        [Entry("material_library")]
		public string MaterialLibrary;
	}
}
