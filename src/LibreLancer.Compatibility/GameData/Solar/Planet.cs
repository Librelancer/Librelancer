// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Ini;

namespace LibreLancer.Compatibility.GameData.Solar
{
	public class Planet : Archetype
	{
		public List<int> LodRanges { get; private set; }
		public float? HitPts { get; private set; }

		public Planet(Section section, FreelancerData data)
			: base(section, data)
		{
			foreach (Entry e in section)
			{
				if (!parentEntry(e))
					switch (e.Name.ToLowerInvariant())
				{
				case "lodranges":
					if (LodRanges != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					LodRanges = new List<int>();
					foreach (IValue i in e) LodRanges.Add(i.ToInt32());
					break;
				case "hit_pts":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (HitPts != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					HitPts = e[0].ToSingle();
					break;
				default:
					//throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
					break;
				}
			}
		}
	}
}