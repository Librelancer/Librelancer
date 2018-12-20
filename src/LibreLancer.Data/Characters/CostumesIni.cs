// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LibreLancer.Ini;

namespace LibreLancer.Data.Characters
{
	public class CostumesIni : IniFile
	{
		public List<Costume> Costumes { get; private set; }

		public CostumesIni(string path, FreelancerData gdata)
		{
			Costumes = new List<Costume>();

			foreach (Section s in ParseFile(path))
			{
				switch (s.Name.ToLowerInvariant())
				{
				case "costume":
					Costumes.Add(new Costume(s, gdata));
					break;
				default: throw new Exception("Invalid Section in " + path + ": " + s.Name);
				}
			}
		}

		public Costume FindCostume(string nickname)
		{
			IEnumerable<Costume> candidates = from Costume c in Costumes where c.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase) select c;
			int count = candidates.Count<Costume>();
			if (count == 1) return candidates.First<Costume>();
			else if (count == 0) return null;
			else throw new Exception(count + " Costumes with nickname " + nickname);
		}
	}
}
