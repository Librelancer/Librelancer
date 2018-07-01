/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData
{
	public class MBasesIni : IniFile
	{
		public List<MBase> Bases = new List<MBase>();
		int i;
		public MBasesIni()
		{
            var sections = ParseFile("DATA\\MISSIONS\\mbases.ini").ToArray();
			for (i = 0; i < sections.Length; i++) {
				if (sections[i].Name.ToLowerInvariant() == "mbase")
				{
					Bases.Add(new MBase(EnumerateSections(sections)));
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

		public MBase FindBase(string nickname)
		{
			var n = nickname.ToLowerInvariant();
			var result = from MBase b in Bases where b.Nickname.ToLowerInvariant() == n select b;
			if (result.Count<MBase>() == 1) return result.First<MBase>();
			else return null;
		}
	}
}
