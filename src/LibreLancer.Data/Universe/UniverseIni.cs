// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public class UniverseIni : IniFile
	{
		public int? SecondsPerDay { get; private set; }

		public List<Base> Bases { get; private set; }
		public List<StarSystem> Systems { get; private set; }

		public UniverseIni(string path, FreelancerData freelancerIni)
		{
            Bases = new List<Base>();
            Systems = new List<StarSystem>();

            List<Section> baseSections = new List<Section>();
            List<Section> systemSections = new List<Section>();
			foreach (Section s in ParseFile(path, freelancerIni.VFS))
			{
				switch (s.Name.ToLowerInvariant())
				{
				case "time":
					foreach (Entry e in s)
					{
						switch (e.Name.ToLowerInvariant())
						{
						case "seconds_per_day":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (SecondsPerDay != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
							SecondsPerDay = e[0].ToInt32();
							break;
						default: throw new Exception("Invalid Entry in " + s.Name + ": " + e.Name);
						}
					}
					break;
				case "base":
                    baseSections.Add(s);
					break;
				case "system":
                    systemSections.Add(s);
					break;
				default: throw new Exception("Invalid Section in " + path + ": " + s.Name);
				}
			}


            foreach (var b in baseSections)
                Bases.Add(new Base(b, freelancerIni));
            foreach(var s in systemSections)
                Systems.Add(new StarSystem(freelancerIni.VFS.RemovePathComponent(path), s, freelancerIni));
        }
    }
}
