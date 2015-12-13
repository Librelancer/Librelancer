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

using LibreLancer.Ini;

namespace LibreLancer.Compatibility.GameData.Characters
{
	public class Accessory
	{
		public string Nickname { get; private set; }
		FreelancerData GameData;
		public string MeshPath = null;

		public Accessory(Section s, FreelancerData gdata)
		{
			GameData = gdata;
			foreach (Entry e in s)
			{
				switch (e.Name.ToLowerInvariant())
				{
				case "nickname":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
					if (Nickname != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
					Nickname = e[0].ToString();
					break;
				case "mesh":
					if (e.Count != 1)
						throw new Exception ("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
					if (MeshPath != null)
						throw new Exception ("Duplicate " + e.Name + " Entry in " + s.Name);
					MeshPath = VFS.GetPath (GameData.Freelancer.DataPath + e [0].ToString ());
					break;
				case "hardpoint":
					// TODO: Accessory hardpoint
					break;
				case "body_hardpoint":
					// TODO: Accessory body_hardpoint
					break;
				default: throw new Exception("Invalid Entry in " + s.Name + ": " + e.Name);
				}
			}
		}
	}
}
