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
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData
{
	public class PetalDbIni : IniFile
	{
		public Dictionary<string, string> Rooms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		public Dictionary<string, string> Props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		public Dictionary<string, string> Carts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public void AddFile(string path)
		{
			foreach (var section in ParseFile(path))
			{
				if (!section.Name.Equals("objecttable", StringComparison.OrdinalIgnoreCase))
					throw new Exception("Unexpected section in PetalDB " + section);
				foreach (var e in section)
				{
					switch (e.Name.ToLowerInvariant())
					{
						case "room":
							Rooms.Add(e[0].ToString(), e[1].ToString());
							break;
						case "prop":
							Props.Add(e[0].ToString(), e[1].ToString());
							break;
						case "cart":
							Carts.Add(e[0].ToString(), e[1].ToString());
							break;
					}
				}
			}
		}
	}
}
