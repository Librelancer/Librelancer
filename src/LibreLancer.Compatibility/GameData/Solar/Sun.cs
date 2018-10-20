// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Ini;

namespace LibreLancer.Compatibility.GameData.Solar
{
	public class Sun : Archetype
	{
		public Sun(Section section, FreelancerData data)
			: base(section, data)
		{
			foreach (Entry e in section)
			{
				if (!parentEntry(e)) FLLog.Error("Solar", "Invalid Entry in " + section.Name + ": " + e.Name);
			}
		}
	}
}