// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Text;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Save;

[ParsedSection]
public partial class LockedGates : IWriteSection
{
    [Entry("npc_locked_gate", Multiline = true)]
    public List<int> NpcLockedGates = [];

    [Entry("locked_gate", Multiline = true)]
    public List<int> PlayerLockedGates = [];


    public void WriteTo(IniBuilder builder)
    {
        var sec = builder.Section("locked_gates");
        foreach (var g in NpcLockedGates)
            sec.Entry("npc_locked_gate", (uint) g);
        foreach (var g in PlayerLockedGates)
            sec.Entry("locked_gate", (uint) g);
        sec.RemoveIfEmpty();
    }
}