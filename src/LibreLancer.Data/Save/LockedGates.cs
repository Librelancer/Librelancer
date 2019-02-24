// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Data.Save
{
    public class LockedGates
    {
        [Entry("npc_locked_gate", Multiline = true)]
        public List<int> NpcLockedGates = new List<int>();
    }
}
