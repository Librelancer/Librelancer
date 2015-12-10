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
using LibreLancer.Utf.Dfm;

namespace LibreLancer.GameData.Characters
{
	public class Bodypart
	{
		public string Nickname { get; private set; }
		FreelancerData gameData;
		private string meshPath = null;
		private DfmFile mesh = null;
		public DfmFile Mesh
		{
			get
			{
				if (mesh == null) mesh = new DfmFile(gameData.Freelancer.DataPath + meshPath, null);
				return mesh;
			}
		}

		public Bodypart(Section s, FreelancerData gdata)
		{
			gameData = gdata;
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
					if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
					if (meshPath != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
					meshPath = e[0].ToString();
					break;
				default: throw new Exception("Invalid Entry in " + s.Name + ": " + e.Name);
				}
			}
		}
	}
}
