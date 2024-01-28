// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;
namespace LibreLancer.Data
{
	public class BaseNavBarIni : IniFile
	{
		public Dictionary<string, string> Navbar = new Dictionary<string, string>();
		public BaseNavBarIni(string datapath, FileSystem vfs)
		{
			foreach (Section s in ParseFile(datapath + "INTERFACE\\BASESIDE\\navbar.ini", vfs, true))
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
