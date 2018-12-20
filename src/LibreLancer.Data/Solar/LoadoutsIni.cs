// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Ini;

namespace LibreLancer.Data.Solar
{
	public class LoadoutsIni : IniFile
	{
		public List<Loadout> Loadouts { get; private set; }

		public LoadoutsIni()
		{
			Loadouts = new List<Loadout>();
		}

		public void AddLoadoutsIni(string path, FreelancerData gdata)
		{
			foreach (Section s in ParseFile(path))
			{
				switch (s.Name.ToLowerInvariant())
				{
				case "loadout":
					Loadouts.Add(new Loadout(s, gdata));
					break;
				default:
					throw new Exception("Invalid Section in " + path + ": " + s.Name);
				}
			}
		}

		public Loadout FindLoadout(string nickname)
		{
			IEnumerable<Loadout> candidates = from Loadout s in Loadouts where s.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase) select s;
			int count = candidates.Count<Loadout>();
			if (count == 1) return candidates.First<Loadout>();
			else if (count == 0) return null;
			else throw new Exception(count + " Loadouts with nickname " + nickname);
		}
	}
}