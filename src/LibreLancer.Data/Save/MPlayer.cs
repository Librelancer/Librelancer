// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Text;
using LibreLancer.Data.Universe.Rooms;
using LibreLancer.Ini;

namespace LibreLancer.Data.Save
{
    public record SaveItemCount(HashValue Item, int Count);

    public record VNPC(HashValue ItemA, HashValue ItemB, int Unknown1, int Unknown2);
    public class MPlayer : IWriteSection
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
        [Entry("holes_visited", Multiline = true)]
        public List<int> HolesVisited = new List<int>();
        [Entry("locked_gate", Multiline = true)]
        public List<int> LockedGates = new List<int>();

        public List<SaveItemCount> ShipTypeKilled = new List<SaveItemCount>();
        public List<SaveItemCount> RmCompleted = new List<SaveItemCount>();
        public List<VNPC> VNPCs = new List<VNPC>();

        [EntryHandler("vnpc", MinComponents = 4, Multiline = true)]
        void HandleVNPC(Entry e) =>
            VNPCs.Add(new VNPC(new HashValue(e[0]), new HashValue(e[1]), e[2].ToInt32(), e[3].ToInt32()));

        [EntryHandler("ship_type_killed", MinComponents = 2, Multiline = true)]
        void HandleShipKill(Entry e) => ShipTypeKilled.Add(new SaveItemCount(new HashValue(e[0]), e[1].ToInt32()));

        [EntryHandler("rm_completed", MinComponents = 2, Multiline = true)]
        void HandleRm(Entry e) => RmCompleted.Add(new SaveItemCount(new HashValue(e[0]), e[1].ToInt32()));


        public void WriteTo(IniBuilder builder)
        {
            var sec = builder.Section("mPlayer");
            foreach (var gate in LockedGates)
                sec.Entry("locked_gate", (uint) gate);
            sec.Entry("can_dock", CanDock);
            sec.Entry("can_tl", CanTl);
            foreach (var s in ShipTypeKilled)
            {
                sec.Entry("ship_type_killed", (uint)s.Item, s.Count);
            }
            foreach (var r in RmCompleted)
            {
                sec.Entry("rm_completed", (uint)r.Item, r.Count);
            }
            foreach(var v in VNPCs)
            {
                sec.Entry("vnpc", (uint)v.ItemA, (uint)v.ItemB, v.Unknown1, v.Unknown2);
            }

            sec.Entry("total_cash_earned", TotalCashEarned);
            sec.Entry("total_time_played", TotalTimePlayed);
            foreach (var s in SysVisited)
               sec.Entry("sys_visited", (uint) s);
            foreach (var b in BaseVisited)
                sec.Entry("base_visited", (uint) b);
            foreach (var h in HolesVisited)
                sec.Entry("holes_visited", (uint) h);
        }
    }
}
