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
        public Dictionary<string, Loadout> Loadouts = new Dictionary<string, Loadout>(StringComparer.OrdinalIgnoreCase);

		public void AddLoadoutsIni(string path, FreelancerData gdata)
		{
			foreach (Section s in ParseFile(path))
			{
				switch (s.Name.ToLowerInvariant())
				{
                    case "loadout":
                        var l = new Loadout(s, gdata);
                        if(string.IsNullOrEmpty(l.Nickname))
                        {
                            FLLog.Error("Loadouts", "Loadout without name at " + s.File + ":" + s.Line);
                        } else
                            Loadouts[l.Nickname] = l;
					break;
				default:
					throw new Exception("Invalid Section in " + path + ": " + s.Name);
				}
			}
		}

		public Loadout FindLoadout(string nickname)
		{
            if (nickname == null) return null;
            Loadout l;
            Loadouts.TryGetValue(nickname, out l);
            return l;
		}
	}
}