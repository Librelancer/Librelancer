// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Pilots;

[ParsedSection]
public partial class Pilot
{
    [Entry("nickname", Required = true)] public string Nickname = null!;
    [Entry("inherit")] public string? Inherit;
    [Entry("gun_id")] public string? GunId;
    [Entry("missile_id")] public string? MissileId;
    [Entry("evade_dodge_id")] public string? EvadeDodgeId;
    [Entry("evade_break_id")] public string? EvadeBreakId;
    [Entry("buzz_head_toward_id")] public string? BuzzHeadTowardId;
    [Entry("buzz_pass_by_id")] public string? BuzzPassById;
    [Entry("trail_id")] public string? TrailId;
    [Entry("strafe_id")] public string? StrafeId;
    [Entry("engine_kill_id")] public string? EngineKillId;
    [Entry("mine_id")] public string? MineId;
    [Entry("countermeasure_id")] public string? CountermeasureId;
    [Entry("damage_reaction_id")] public string? DamageReactionId;
    [Entry("missile_reaction_id")] public string? MissileReactionId;
    [Entry("formation_id")] public string? FormationId;
    [Entry("repair_id")] public string? RepairId;
    [Entry("job_id")] public string? JobId;
}
