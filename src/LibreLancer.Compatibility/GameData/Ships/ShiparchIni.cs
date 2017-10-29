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
namespace LibreLancer.Compatibility.GameData.Ships
{
	public class ShiparchIni : IniFile
	{
		public List<Ship> Ships = new List<Ship>();

		public ShiparchIni ()
		{
		}
		public void AddShiparchIni(string path, FreelancerData fldata)
		{
			foreach (Section s in ParseFile(path)) {
				if (s.Name.ToLowerInvariant () == "ship")
					Ships.Add (new Ship (s, fldata));
			}
		}

		public Ship GetShip(string name)
		{
			
			IEnumerable<Ship> result = from Ship s in Ships where s.Nickname == name select s;
			if (result.Count<Ship> () == 1)
				return result.First<Ship> ();
			else
				throw new Exception ();
		}
	}
}

