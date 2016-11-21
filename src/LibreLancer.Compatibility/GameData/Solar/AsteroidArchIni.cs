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
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Solar
{
	public class AsteroidArchIni : IniFile
	{
		public List<Asteroid> Asteroids = new List<Asteroid> ();
		public void AddFile (string path)
		{
			foreach (var s in ParseFile(path)) {
				switch (s.Name.ToLowerInvariant ()) {
				case "asteroid":
				case "asteroidmine":
					Asteroids.Add (new Asteroid (s));
					break;
				}
			}
		}
		public Asteroid FindAsteroid(string nickname)
		{
			IEnumerable<Asteroid> candidates = from Asteroid s in Asteroids where s.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase) select s;
			int count = candidates.Count<Asteroid>();
			if (count == 1) return candidates.First<Asteroid>();
			else if (count == 0) return null;
			else throw new Exception(count + " Asteroids with nickname " + nickname);
		}
	}
}

