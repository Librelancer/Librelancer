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
	public class Loadout : ICustomEntryHandler
    {
        [Entry("nickname", Required =  true)] public string Nickname;

        [Entry("archetype")] public string Archetype;

        public List<LoadoutCargo> Cargo = new List<LoadoutCargo>();
        public List<LoadoutEquip> Equip = new List<LoadoutEquip>();

        private static readonly CustomEntry[] _custom = new CustomEntry[]
        {
            new("cargo", (s,e) => ((Loadout)s).Cargo.Add(new LoadoutCargo(e))),
            new("equip", (s,e) => ((Loadout)s).Equip.Add(new LoadoutEquip(e))),
            new("hull", CustomEntry.Ignore),
            new("addon", CustomEntry.Ignore),
            new("hull_damage", CustomEntry.Ignore)
        };

        IEnumerable<CustomEntry> ICustomEntryHandler.CustomEntries => _custom;

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
