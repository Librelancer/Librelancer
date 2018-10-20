// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Equipment
{
	public class InternalFx : AbstractEquipment
	{
		public string UseAnimation;
		public string UseSound;
		public InternalFx(Section section, FreelancerData gdata)
			: base(section)
		{
			foreach (Entry e in section)
			{
				if (!parentEntry(e))
				{
					switch (e.Name.ToLowerInvariant())
					{
						case "use_animation":
							UseAnimation = e[0].ToString();
							break;
						case "use_sound":
							UseSound = e[0].ToString();
							break;
					}
				}
			}
		}
	}
}
