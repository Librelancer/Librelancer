// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Collections.Generic;
using LibreLancer.Ini;

namespace LibreLancer.Data.Pilots
{
    public class EvadeBreakBlock : PilotBlock, ICustomEntryHandler
    {
        [Entry("evade_break_roll_throttle")] public float RollThrottle;
        [Entry("evade_break_time")] public float Time;
        [Entry("evade_break_interval_time")] public float IntervalTime;
        [Entry("evade_break_afterburner_delay")]
        public float AfterburnerDelay;
        [Entry("evade_break_afterburner_delay_variance_percent")]
        public float AfterburnerDelayVariancePercent;
        [Entry("evade_break_attempt_reverse_time")]
        public float AttemptReverseTime;
        [Entry("evade_break_reverse_distance")]
        public float ReverseDistance;

        [Entry("evade_break_turn_throttle")] public float TurnThrottle;

        public List<DirectionWeight> DirectionWeights = new List<DirectionWeight>();
        public List<EvadeBreakStyle> StyleWeights = new List<EvadeBreakStyle>();

        
        private static readonly CustomEntry[] _custom = new CustomEntry[]
        {
            new(
                "evade_break_style_weight",
                (s, e) =>
                    ((EvadeBreakBlock) s).StyleWeights.Add(new EvadeBreakStyle(e))),
            new("evade_break_direction_weight",
                (s, e) => ((EvadeBreakBlock) s).DirectionWeights.Add(new DirectionWeight(e)))

        };

        IEnumerable<CustomEntry> ICustomEntryHandler.CustomEntries => _custom;
    }
    
    public class EvadeBreakStyle
    {
        public string Style;
        public float Weight;

        public EvadeBreakStyle()
        {
        }

        public EvadeBreakStyle(Entry e)
        {
            Style = e[0].ToString();
            Weight = e[1].ToSingle();
        }
    }
}