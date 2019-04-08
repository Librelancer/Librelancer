// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Ini;
namespace LibreLancer.Data
{
	public class MBasesIni : IniFile
	{
        public Dictionary<string, MBase> Bases = new Dictionary<string, MBase>(StringComparer.OrdinalIgnoreCase);
		int i;
		public MBasesIni()
		{
            var sections = ParseFile("DATA\\MISSIONS\\mbases.ini").ToArray();
			for (i = 0; i < sections.Length; i++) {
				if (sections[i].Name.ToLowerInvariant() == "mbase")
				{
                    var mb = new MBase(EnumerateSections(sections));
                    Bases.Add(mb.Nickname, mb);
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
