﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data
{
    [ParsedSection]
	public partial class MRoom
	{
        [Entry("nickname")]
		public string Nickname;
        [Entry("character_density")]
		public int CharacterDensity;
		public List<MRoomNpcRef> NPCs = new List<MRoomNpcRef>();

        [EntryHandler("fixture", MinComponents = 2, Multiline = true)]
        void HandleFixture(Entry e) => NPCs.Add(new MRoomNpcRef(e));
    }

	public class MRoomNpcRef
	{
		public string Npc;
		public string StandMarker;
		public string Script;
		public string Action;

		public MRoomNpcRef() { }
        public MRoomNpcRef(Entry e)
		{
			Npc = e[0].ToString();
			StandMarker = e[1].ToString();
            if(e.Count > 2)
			    Script = e[2].ToString();
            if(e.Count > 3)
			    Action = e[3].ToString();
		}
	}
}
