// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Data.Save
{
    public class MPlayer
    {
        [Entry("can_dock")]
        public int CanDock;
        [Entry("can_tl")]
        public int CanTl;
        [Entry("total_cash_earned")]
        public float TotalCashEarned;
        [Entry("total_time_played")]
        public float TotalTimePlayed;
        [Entry("sys_visited")]
        public int SysVisited;
        [Entry("base_visited")]
        public int BaseVisited;
        [Entry("locked_gate", Multiline = true)]
        public List<int> LockedGates = new List<int>();

        bool HandleEntry(Entry e)
        {
            if(e.Name.Equals("vnpc", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }
    }
}
