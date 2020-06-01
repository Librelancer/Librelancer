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
        [Section("loadout")]
        public List<Loadout> Loadouts = new List<Loadout>();

		public void AddLoadoutsIni(string path, FreelancerData gdata)
        {
            ParseAndFill(path, gdata.VFS);
        }
    }
}