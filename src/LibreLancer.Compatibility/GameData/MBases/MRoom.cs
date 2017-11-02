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
	public class MRoom
	{
		public string Nickname;
		public int CharacterDensity;
		public List<MRoomNpcRef> NPCs = new List<MRoomNpcRef>();
		public MRoom(Section section)
		{
			foreach (var e in section)
			{
				switch (e.Name.ToLowerInvariant())
				{
					case "nickname":
						Nickname = e[0].ToString();
						break;
					case "character_density":
						CharacterDensity = e[0].ToInt32();
						break;
					case "fixture":
						NPCs.Add(new MRoomNpcRef(e));
						break;
				}
			}
		}
	}
	public class MRoomNpcRef
	{
		public string Npc;
		public string StandMarker;
		public string Script;
		public string Action;

		public MRoomNpcRef(Entry e)
		{
			Npc = e[0].ToString();
			StandMarker = e[1].ToString();
			Script = e[2].ToString();
			Action = e[3].ToString();
		}
	}
}
