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
	public class StararchIni : IniFile
	{
		public List<Star> Stars { get; private set; }

		public StararchIni(string path)
		{
			Stars = new List<Star>();

			foreach (Section s in ParseFile(path))
			{
				switch (s.Name.ToLowerInvariant())
				{
				case "texture":
					// TODO
					break;
				case "star":
					Stars.Add(new Star(this, s));
					break;
				case "star_glow":
					// TODO
					break;
				case "lens_flare":
					// TODO
					break;
				case "lens_glow":
					// TODO
					break;
				case "spines":
					// TODO
					break;
				default:
					throw new Exception("Invalid Section in " + path + ": " + s.Name);
				}
			}
		}

		public Star FindStar(string nickname)
		{
			return (from Star s in Stars where s.Nickname == nickname select s).First<Star>();
		}
	}
}