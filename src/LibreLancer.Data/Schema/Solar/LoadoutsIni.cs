// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Solar
{
    [ParsedIni]
	public partial class LoadoutsIni
	{
        [Section("loadout")]
        public List<Loadout> Loadouts = new List<Loadout>();

		public void AddLoadoutsIni(string path, FreelancerData gdata, IniStringPool stringPool = null)
        {
            ParseIni(path, gdata.VFS, stringPool);
        }
    }
}
