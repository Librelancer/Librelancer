// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Equipment
{
	public class PowerCore : AbstractEquipment
	{
		public string DaArchetype;
		public string MaterialLibrary;
		public PowerCore(Section section)
			: base(section)
		{
			foreach (var e in section)
			{
				if (!parentEntry(e))
				{
					switch (e.Name.ToLowerInvariant())
					{
						case "da_archetype":
							DaArchetype = e[0].ToString();
							break;
						case "material_library":
							MaterialLibrary = e[0].ToString();
							break;
					}
				}
			}
		}
	}
}
