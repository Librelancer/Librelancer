// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Collections.Generic;
using System.Text;
using LibreLancer.Ini;

namespace LibreLancer.Data.Save
{
    public record SaveItemCount(int Item, int Count)
    {
        public static bool FromEntry(Entry e, out SaveItemCount count)
        {
            if (e.Count != 2)
            {
                count = null;
                FLLog.Warning("Ini", $"Invalid save line: {e} in {e.File}:{e.Line}");
                return false;
            }
            count = new SaveItemCount(e[0].ToInt32(), e[1].ToInt32());
            return true;
        }
    }
    public class MPlayer : ICustomEntryHandler, IWriteSection
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

        private static readonly CustomEntry[] _custom = new CustomEntry[]
        {
            new("vnpc", CustomEntry.Ignore),
            new ("ship_type_killed", (h, e) =>
            {
                if(SaveItemCount.FromEntry(e, out var count))
                    ((MPlayer)h).ShipTypeKilled.Add(count);
            }),
            new ("rm_completed", (h, e) =>
            {
                if(SaveItemCount.FromEntry(e, out var count))
                    ((MPlayer)h).RmCompleted.Add(count);
            })
        };

        IEnumerable<CustomEntry> ICustomEntryHandler.CustomEntries => _custom;


        public void WriteTo(StringBuilder builder)
        {
            builder.AppendLine("[mPlayer]");
            foreach (var gate in LockedGates)
                builder.AppendEntry("locked_gate", (uint) gate);
            builder.AppendEntry("can_dock", CanDock);
            builder.AppendEntry("can_tl", CanTl);
            foreach (var s in ShipTypeKilled)
            {
                builder.Append("ship_type_killed = ")
                    .Append((uint) s.Item)
                    .Append(", ")
                    .AppendLine(s.Count.ToString());
            }
            foreach (var r in RmCompleted)
            {
                builder.Append("rm_completed = ")
                    .Append((uint) r.Item)
                    .Append(", ")
                    .AppendLine(r.Count.ToString());
            }
            builder.AppendEntry("total_cash_earned", TotalCashEarned);
            builder.AppendEntry("total_time_played", TotalTimePlayed);
            foreach (var s in SysVisited)
                builder.AppendEntry("sys_visited", (uint) s);
            foreach (var b in BaseVisited)
                builder.AppendEntry("base_visited", (uint) b);
            foreach (var h in HolesVisited)
                builder.AppendEntry("holes_visited", (uint) h);
            builder.AppendLine();
        }
    }
}
