// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Data
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
