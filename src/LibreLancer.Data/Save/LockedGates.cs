// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Text;
using LibreLancer.Ini;
namespace LibreLancer.Data.Save
{
    public class LockedGates : IWriteSection
    {
        [Entry("npc_locked_gate", Multiline = true)]
        public List<int> NpcLockedGates = new List<int>();

        [Entry("locked_gate", Multiline = true)]
        public List<int> PlayerLockedGates = new List<int>();


        public void WriteTo(StringBuilder builder)
        {
            builder.AppendLine("[locked_gates]");
            foreach (var g in NpcLockedGates)
                builder.AppendEntry("npc_locked_gate", (uint) g);
            foreach (var g in PlayerLockedGates)
                builder.AppendEntry("locked_gate", (uint) g);
            builder.AppendLine();
        }
    }
}
