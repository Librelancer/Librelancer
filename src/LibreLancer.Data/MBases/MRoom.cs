// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Data
{
	public class MRoom : ICustomEntryHandler
	{
        [Entry("nickname")]
		public string Nickname;
        [Entry("character_density")]
		public int CharacterDensity;
		public List<MRoomNpcRef> NPCs = new List<MRoomNpcRef>();

        private static CustomEntry[] entries = new CustomEntry[] {
            new("fixture", (m, e) => ((MRoom)m).NPCs.Add(new MRoomNpcRef(e)))
        };

        IEnumerable<CustomEntry> ICustomEntryHandler.CustomEntries => entries;
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
            if(e.Count > 2)
			    Script = e[2].ToString();
            if(e.Count > 3)
			    Action = e[3].ToString();
		}
	}
}
