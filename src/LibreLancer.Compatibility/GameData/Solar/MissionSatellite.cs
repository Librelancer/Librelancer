// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Ini;

namespace LibreLancer.Compatibility.GameData.Solar
{
	public class MissionSatellite : Archetype
	{
		public MissionSatellite(Section section, FreelancerData data)
			: base(section, data)
		{
			foreach (Entry e in section)
			{
				if (!parentEntry(e))
				{
					/*switch (e.Name.ToLowerInvariant())
                    {
                        case "":
                            if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
                            if (x != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
                            x = e[0].ToString();
                            break;
                        default:
                            throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
                    }*/
				}
			}
		}
	}
}