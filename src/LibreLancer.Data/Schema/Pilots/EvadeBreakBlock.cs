// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Pilots
{
    [ParsedSection]
    public partial class EvadeBreakBlock : PilotBlock
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


        [EntryHandler("evade_break_style_weight", MinComponents = 2, Multiline = true)]
        void HandleDodgeStyle(Entry e) => StyleWeights.Add(new EvadeBreakStyle(e));

        [EntryHandler("evade_break_direction_weight", MinComponents = 2, Multiline = true)]
        void HandleDirectionWeight(Entry e) => DirectionWeights.Add(new DirectionWeight(e));
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
