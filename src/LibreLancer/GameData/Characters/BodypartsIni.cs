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

using LibreLancer.Ini;

namespace LibreLancer.GameData.Characters
{
	public class BodypartsIni : IniFile
	{
		public List<Bodypart> Bodyparts { get; private set; }
		public List<Accessory> Accessories { get; private set; }

		public BodypartsIni(string path, FreelancerData gdata)
		{
			Bodyparts = new List<Bodypart>();
			Accessories = new List<Accessory>();

			foreach (Section s in ParseFile(path))
			{
				switch (s.Name.ToLowerInvariant())
				{
				case "animations":
					// TODO: Bodyparts Animations
					break;
				case "detailswitchtable":
					// TODO: Bodyparts DetailSwitchTable
					break;
				case "petalanimations":
					// TODO: Bodyparts PetalAnimations
					break;
				case "skeleton":
					// TODO: Bodyparts Skeleton
					break;
				case "body":
				case "head":
				case "righthand":
				case "lefthand":
					Bodyparts.Add(new Bodypart(s, gdata));
					break;
				case "accessory":
					Accessories.Add(new Accessory(s, gdata));
					break;
				default: throw new Exception("Invalid Section in " + path + ": " + s.Name);
				}
			}
		}

		public Bodypart FindBodypart(string nickname)
		{
			IEnumerable<Bodypart> candidates = from Bodypart b in Bodyparts where b.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase) select b;
			int count = candidates.Count<Bodypart>();
			if (count == 1) return candidates.First<Bodypart>();
			else if (count == 0) return null;
			else throw new Exception(count + " Bodyparts with nickname " + nickname);
		}

		public Accessory FindAccessory(string nickname)
		{
			IEnumerable<Accessory> candidates = from Accessory a in Accessories where a.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase) select a;
			int count = candidates.Count<Accessory>();
			if (count == 1) return candidates.First<Accessory>();
			else if (count == 0) return null;
			else throw new Exception(count + " Accessories with nickname " + nickname);
		}
	}
}
