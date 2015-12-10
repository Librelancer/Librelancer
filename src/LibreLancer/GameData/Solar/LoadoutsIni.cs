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
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Ini;

namespace LibreLancer.GameData.Solar
{
	public class LoadoutsIni : IniFile
	{
		public List<Loadout> Loadouts { get; private set; }

		public LoadoutsIni()
		{
			Loadouts = new List<Loadout>();
		}

		public void AddLoadoutsIni(string path, FreelancerData gdata)
		{
			foreach (Section s in ParseFile(path))
			{
				switch (s.Name.ToLowerInvariant())
				{
				case "loadout":
					Loadouts.Add(new Loadout(s, gdata));
					break;
				default:
					throw new Exception("Invalid Section in " + path + ": " + s.Name);
				}
			}
		}

		public Loadout FindLoadout(string nickname)
		{
			IEnumerable<Loadout> candidates = from Loadout s in Loadouts where s.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase) select s;
			int count = candidates.Count<Loadout>();
			if (count == 1) return candidates.First<Loadout>();
			else if (count == 0) return null;
			else throw new Exception(count + " Loadouts with nickname " + nickname);
		}
	}
}