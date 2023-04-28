// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Ini;
namespace LibreLancer.Data
{
	public class MBase : IniFile
	{
		public string Nickname;
		public string LocalFaction;
        public int Diff;
        public string MsgIdPrefix;
        
        public MVendor MVendor;
		public List<MRoom> Rooms = new List<MRoom>();
		public List<GfNpc> Npcs = new List<GfNpc>();
        public List<BaseFaction> Factions = new List<BaseFaction>();
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
                                case "diff":
                                    Diff = e[0].ToInt32();
                                    break;
                                case "msg_id_prefix":
                                    MsgIdPrefix = e[0].ToString();
                                    break;
                                default:
                                    IniWarning.UnknownEntry(e, s);
                                    break;
							}
                        }
						break;
                    case "mvendor":
                        MVendor = FromSection<MVendor>(s);
                        break;
					case "mroom":
                        Rooms.Add(FromSection<MRoom>(s));
						break;
					case "gf_npc":
                        Npcs.Add(FromSection<GfNpc>(s));
						break;
                    case "basefaction":
                        Factions.Add(FromSection<BaseFaction>(s));
                        break;
                    default:
                        IniWarning.UnknownSection(s);
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
