// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.MBases
{
    [ParsedSection]
	public partial class MBase
	{
        [Entry("nickname")]
		public string Nickname;
        [Entry("local_faction")]
		public string LocalFaction;
        [Entry("diff")]
        public int Diff;
        [Entry("msg_id_prefix")]
        public string MsgIdPrefix;

        [Section("mvendor", Child = true)]
        public MVendor MVendor;
        [Section("mroom", Child = true)]
		public List<MRoom> Rooms = new List<MRoom>();
        [Section("gf_npc", Child = true)]
		public List<GfNpc> Npcs = new List<GfNpc>();
        [Section("basefaction", Child = true)]
        public List<BaseFaction> Factions = new List<BaseFaction>();
	}
}
