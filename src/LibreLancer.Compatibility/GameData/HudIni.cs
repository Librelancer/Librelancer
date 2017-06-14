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
using LibreLancer.Compatibility.GameData.Interface;
namespace LibreLancer.Compatibility.GameData
{
	public class HudIni : IniFile
	{
		public List<HudManeuver> Maneuvers = new List<HudManeuver>();
		public HudIni()
		{
		}
		public void AddIni(string path)
		{
			foreach (var section in ParseFile(path))
			{
				switch (section.Name.ToLowerInvariant())
				{
					case "maneuvers":
						foreach (var e in section)
							Maneuvers.Add(new HudManeuver(e));
						break;
				}
			}
		}
	}
}