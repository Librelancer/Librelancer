// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Equipment
{
	public class AttachedFx : AbstractEquipment
	{
		public string Particles;
		public AttachedFx(Section section, FreelancerData gdata)
			: base(section)
		{
			foreach (Entry e in section)
			{
				if (!parentEntry(e))
				{
					switch (e.Name.ToLowerInvariant())
					{
						case "particles":
							Particles = e[0].ToString();
							break;
					}
				}
			}
		}
	}
}
