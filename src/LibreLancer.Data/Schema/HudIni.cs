// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;
using LibreLancer.Data.Schema.Interface;

namespace LibreLancer.Data.Schema
{
	public class HudIni
	{
		public List<HudManeuver> Maneuvers = new List<HudManeuver>();
		public HudIni()
		{
		}
		public void AddIni(string path, FileSystem vfs, IniStringPool stringPool = null)
		{
			foreach (var section in IniFile.ParseFile(path, vfs, false, stringPool))
			{
				switch (section.Name.ToLowerInvariant())
				{
					case "maneuvers":
						foreach (var e in section)
							Maneuvers.Add(new HudManeuver(e));
						break;
				}
			}
		}
	}
}
