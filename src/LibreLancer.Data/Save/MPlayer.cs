// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Data.Save
{
    public class MPlayer : ICustomEntryHandler
    {
        [Entry("can_dock")]
        public int CanDock;
        [Entry("can_tl")]
        public int CanTl;
        [Entry("total_cash_earned")]
        public float TotalCashEarned;
        [Entry("total_time_played")]
        public float TotalTimePlayed;
        [Entry("sys_visited", Multiline = true)]
        public List<int> SysVisited = new List<int>();
        [Entry("base_visited", Multiline = true)] 
        public List<int> BaseVisited = new List<int>();
        [Entry("locked_gate", Multiline = true)]
        public List<int> LockedGates = new List<int>();

        private static readonly CustomEntry[] _custom = new CustomEntry[]
        {
            new("vnpc", CustomEntry.Ignore),
        };

        IEnumerable<CustomEntry> ICustomEntryHandler.CustomEntries => _custom;
    }
}
