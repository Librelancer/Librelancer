// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Ini;

namespace LibreLancer.Data.Solar
{
	public class SolararchIni : IniFile
	{
		public List<Archetype> Solars { get; private set; }

		public SolararchIni(string path, FreelancerData gameData)
		{
			Solars = new List<Archetype>();
            bool lastNull = false;
			foreach (Section s in ParseFile(path))
			{
                switch (s.Name.ToLowerInvariant())
                {
                    case "solar":
                        Solars.Add(FromSection<Archetype>(s));
					break;
				case "collisiongroup":
                        if(!lastNull)
					Solars.Last<Archetype>().CollisionGroups.Add(new CollisionGroup(s));
					break;
				default:
					throw new Exception("Invalid Section in " + path + ": " + s.Name);
				}
			}
		}

		public Archetype FindSolar(string nickname)
		{
			IEnumerable<Archetype> candidates = from Archetype s in Solars where s.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase) select s;
			int count = candidates.Count<Archetype>();
			if (count == 1) return candidates.First<Archetype>();
			else if (count == 0) return null;
			else throw new Exception(count + " Archetypes with nickname " + nickname);
		}
	}
}
