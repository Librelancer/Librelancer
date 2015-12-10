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
 * Portions created by the Initial Developer are Copyright (C) 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LibreLancer.Ini;

namespace LibreLancer.GameData.Characters
{
	public class CostumesIni : IniFile
	{
		public List<Costume> Costumes { get; private set; }

		public CostumesIni(string path, FreelancerData gdata)
		{
			Costumes = new List<Costume>();

			foreach (Section s in ParseFile(path))
			{
				switch (s.Name.ToLowerInvariant())
				{
				case "costume":
					Costumes.Add(new Costume(s, gdata));
					break;
				default: throw new Exception("Invalid Section in " + path + ": " + s.Name);
				}
			}
		}

		public Costume FindCostume(string nickname)
		{
			IEnumerable<Costume> candidates = from Costume c in Costumes where c.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase) select c;
			int count = candidates.Count<Costume>();
			if (count == 1) return candidates.First<Costume>();
			else if (count == 0) return null;
			else throw new Exception(count + " Costumes with nickname " + nickname);
		}
	}
}
