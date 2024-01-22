// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Ini;

using LibreLancer.Data.Solar;
using LibreLancer.Data.Characters;

namespace LibreLancer.Data.Universe
{
	public class SystemObject : SystemPart
    {
        [Entry("ambient_color")]
        [Entry("ambient")]
        public Color4? AmbientColor;

        [Entry("archetype")]
        public string Archetype;

        [Entry("star")]
        public string Star;

        [Entry("atmosphere_range")]
        public int? AtmosphereRange;

        [Entry("burn_color")]
        public Color4? BurnColor;

        [Entry("base")]
        public string Base;

        [Entry("msg_id_prefix")]
        public string MsgIdPrefix;

        [Entry("jump_effect")]
        public string JumpEffect;

        [Entry("behavior")]
        public string Behavior;

        [Entry("difficulty_level")]
        public int? DifficultyLevel;
		public JumpReference Goto { get; private set; }

        [Entry("loadout")]
        public string Loadout;

        [Entry("pilot")]
        public string Pilot;

        [Entry("dock_with")]
        public string DockWith;

        [Entry("voice")]
        public string Voice;

        [Entry("space_costume")]
        public string[] SpaceCostume;

        [Entry("faction")]
        public string Faction;

        [Entry("prev_ring")]
        public string PrevRing;

        [Entry("next_ring")]
        public string NextRing;

        [Entry("tradelane_space_name")]
        public int TradelaneSpaceName;

        [Entry("parent")]
        public string Parent;

        [Entry("comment")]
        public string Comment;

        public string RingZone;
        public string RingFile;

        [EntryHandler("goto", MinComponents = 3)]
        void HandleGoto(Entry e) => Goto = new JumpReference(e[0].ToString(), e[1].ToString(), e[2].ToString());

        [EntryHandler("ring", MinComponents = 2)]
        void HandleRing(Entry e)
        {
            RingZone = e[0].ToString();
            RingFile = e[1].ToString();
        }
    }
}
