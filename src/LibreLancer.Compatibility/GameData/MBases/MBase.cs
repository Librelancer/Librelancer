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
namespace LibreLancer.Compatibility.GameData
{
	public class MBase
	{
		public string Nickname;
		public string LocalFaction;
		public List<MRoom> Rooms = new List<MRoom>();
		public List<GfNpc> Npcs = new List<GfNpc>();
		public MBase(IEnumerable<Section> sections)
		{
			foreach (var s in sections)
			{
				switch (s.Name.ToLowerInvariant())
				{
					case "mbase":
						foreach (var e in s)
						{
							switch (e.Name.ToLowerInvariant())
							{
								case "nickname":
									Nickname = e[0].ToString();
									break;
								case "local_faction":
									LocalFaction = e[0].ToString();
									break;
							}
							
						}
						break;
					case "mroom":
						Rooms.Add(new MRoom(s));
						break;
					case "gf_npc":
						Npcs.Add(new GfNpc(s));
						break;
				}
			}
		}

		public MRoom FindRoom(string nickname)
		{
			var n = nickname.ToLowerInvariant();
			var result = from MRoom b in Rooms where b.Nickname.ToLowerInvariant() == n select b;
			if (result.Count<MRoom>() == 1) return result.First<MRoom>();
			else return null;
		}

		public GfNpc FindNpc(string nickname)
		{
			var n = nickname.ToLowerInvariant();
			var result = from GfNpc b in Npcs where b.Nickname.ToLowerInvariant() == n select b;
			if (result.Count<GfNpc>() == 1) return result.First<GfNpc>();
			else return null;
		}
	}
}
