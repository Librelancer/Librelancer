// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.IO;
using LibreLancer.Ini;
namespace LibreLancer.Data
{
	public class MBasesIni : IniFile
	{
        public Dictionary<string, MBase> Bases = new Dictionary<string, MBase>(StringComparer.OrdinalIgnoreCase);
		public MBasesIni() { }

        int i;
        public void AddFile(string path, FileSystem vfs)
        {
            var sections = ParseFile(path, vfs).ToArray();
            for (i = 0; i < sections.Length; i++) {
                if (sections[i].Name.ToLowerInvariant() == "mbase")
                {
                    var mb = new MBase(EnumerateSections(sections));
                    Bases[mb.Nickname] = mb; //Add() won't overwrite duplicates
                    i--;
                }
            }
        }
		IEnumerable<Section> EnumerateSections(Section[] sections)
		{
			yield return sections[i];
			i++;
			while (i < sections.Length && !sections[i].Name.Equals("mbase", StringComparison.OrdinalIgnoreCase))
			{
				yield return sections[i];
				i++;
			}
		}
	}
}
