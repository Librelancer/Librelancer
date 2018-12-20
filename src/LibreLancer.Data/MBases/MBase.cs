// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Ini;
namespace LibreLancer.Data
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
