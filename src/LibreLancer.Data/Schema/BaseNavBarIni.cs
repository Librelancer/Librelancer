// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema
{
	public class BaseNavBarIni
	{
		public Dictionary<string, string> Navbar = new Dictionary<string, string>();
		public BaseNavBarIni(string datapath, FileSystem vfs, IniStringPool stringPool = null)
		{
			foreach (Section s in IniFile.ParseFile(datapath + "INTERFACE\\BASESIDE\\navbar.ini", vfs, true, stringPool))
			{
				if (s.Name.ToLowerInvariant() == "navbar")
				{
					foreach (var e in s)
					{
						Navbar.Add(e.Name, e[0].ToString());
					}
				}
			}
		}
	}
}
