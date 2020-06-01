// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Ini;
using LibreLancer.Data.Equipment;

namespace LibreLancer.Data.Solar
{
	public class Loadout
    {
        [Entry("nickname")] public string Nickname;

        [Entry("archetype")] public string Archetype;
        
        [Entry("cargo", Multiline = true)]
        [Entry("hull", Multiline = true)]
        [Entry("addon", Multiline = true)]
        [Entry("hull_damage", Multiline = true)]
        void Noop(Entry e)
        {
            
        }

        public Dictionary<string, string> Equip = new Dictionary<string, string>();
        private int emptyHp = 1;
        [Entry("equip", Multiline = true)]
        void HandleEquip(Entry e)
        {
            string key = e.Count == 2 ? e[1].ToString() : "__noHardpoint" + (emptyHp++).ToString("d2");
            if (e.Count == 2 && e[1].ToString().Trim() == "") key = "__noHardpoint" + (emptyHp++).ToString("d2");
            if (!Equip.ContainsKey(key)) Equip.Add(key, e[0].ToString());
        }
    }
}
