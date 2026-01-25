// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Schema.Equipment;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Solar;

[ParsedSection]
public partial class Loadout
{
    [Entry("nickname", Required =  true)] public string Nickname = null!;

    [Entry("archetype")] public string? Archetype; // Not required

    public List<LoadoutCargo> Cargo = [];
    public List<LoadoutEquip> Equip = [];

    [EntryHandler("cargo", MinComponents = 1, Multiline = true)]
    private void HandleCargo(Entry e) => Cargo.Add(new LoadoutCargo(e));

    [EntryHandler("equip", MinComponents = 1, Multiline = true)]
    private void HandleEquip(Entry e) => Equip.Add(new LoadoutEquip(e));
}

public class LoadoutEquip
{
    public string Nickname = null!;
    public string? Hardpoint;

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
    public string? Nickname;
    public int Count;
    public LoadoutCargo(Entry e)
    {
        Nickname = e[0].ToString();
        Count = e.Count > 1 ? e[1].ToInt32() : 1;
    }
}
