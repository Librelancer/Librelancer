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
        [Section("costume")]
        public List<Costume> Costumes = new List<Costume>();

		public CostumesIni(string path, FreelancerData gdata)
        {
            ParseAndFill(path, gdata.VFS);
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
