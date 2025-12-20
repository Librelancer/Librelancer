// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Pilots
{
    [ParsedSection]
    public partial class FormationBlock : PilotBlock
    {
        [Entry("force_attack_formation_active_time")]
        public float ForceAttackFormationActiveTime;
        [Entry("force_attack_formation_unactive_time")]
        public float ForceAttackFormationUnactiveTime;
        [Entry("break_formation_damage_trigger_percent")]
        public float BreakFormationDamageTriggerPercent;
        [Entry("break_formation_damage_trigger_time")]
        public float BreakFormationDamageTriggerTime;
        [Entry("break_formation_missile_reaction_time")]
        public float BreakFormationMissileReactionTime;
        [Entry("break_apart_formation_missile_reaction_time")]
        public float BreakApartFormationMissileReactionTime;
        [Entry("break_apart_formation_on_evade_break")]
        public bool BreakApartFormationOnEvadeBreak;
        [Entry("break_formation_on_evade_break_time")]
        public float BreakFormationOnEvadeBreakTime;
        [Entry("formation_exit_top_turn_break_away_throttle")]
        public float FormationExitTopTurnBreakAwayThrottle;
        [Entry("formation_exit_roll_outrun_throttle")]
        public float FormationExitRollOutrunThrottle;
        [Entry("formation_exit_max_time")]
        public float FormationExitMaxTime;
    }
}
