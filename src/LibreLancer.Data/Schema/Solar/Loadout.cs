// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Schema.Equipment;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Solar
{
    [ParsedSection]
	public partial class Loadout
    {
        [Entry("nickname", Required =  true)] public string Nickname;

        [Entry("archetype")] public string Archetype;

        public List<LoadoutCargo> Cargo = new List<LoadoutCargo>();
        public List<LoadoutEquip> Equip = new List<LoadoutEquip>();

        [EntryHandler("cargo", MinComponents = 1, Multiline = true)]
        void HandleCargo(Entry e) => Cargo.Add(new LoadoutCargo(e));

        [EntryHandler("equip", MinComponents = 1, Multiline = true)]
        void HandleEquip(Entry e) => Equip.Add(new LoadoutEquip(e));
    }

    public class LoadoutEquip
    {
        public string Nickname;
        public string Hardpoint;

        public LoadoutEquip()
        {
        }

        public LoadoutEquip(Entry e)
        {
            Nickname = e[0].ToString();
            if (e.Count > 1)
                Hardpoint = e[1].ToString();
        }
    }

    public class LoadoutCargo
    {
        public string Nickname;
        public int Count;
        public LoadoutCargo(Entry e)
        {
            Nickname = e[0].ToString();
            if (e.Count > 1)
                Count = e[1].ToInt32();
            else
                Count = 1;
        }
    }
}
