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
 * Portions created by the Initial Developer are Copyright (C) 2011, 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Ini;

namespace LibreLancer.GameData.Solar
{
	public class SolararchIni : IniFile
	{
		public List<Archetype> Solars { get; private set; }

		public SolararchIni(string path, FreelancerData gameData)
		{
			Solars = new List<Archetype>();

			foreach (Section s in ParseFile(path))
			{
				switch (s.Name.ToLowerInvariant())
				{
				case "solar":
					Solars.Add(Archetype.FromSection(s, gameData));
					break;
				case "collisiongroup":
					Solars.Last<Archetype>().CollisionGroups.Add(new CollisionGroup(s));
					break;
				default:
					throw new Exception("Invalid Section in " + path + ": " + s.Name);
				}
			}
		}

		public Archetype FindSolar(string nickname)
		{
			IEnumerable<Archetype> candidates = from Archetype s in Solars where s.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase) select s;
			int count = candidates.Count<Archetype>();
			if (count == 1) return candidates.First<Archetype>();
			else if (count == 0) return null;
			else throw new Exception(count + " Archetypes with nickname " + nickname);
		}
	}
}
