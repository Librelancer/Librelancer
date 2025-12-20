// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Equipment
{
    [ParsedSection]
    public partial class Engine : AbstractEquipment
    {
        [Entry("max_force")] public float MaxForce;
        [Entry("linear_drag")] public float LinearDrag;
        [Entry("power_usage")] public float PowerUsage;
        [Entry("reverse_fraction")] public float ReverseFraction;
        [Entry("flame_effect")] public string FlameEffect;
        [Entry("trail_effect")] public string TrailEffect;
        [Entry("trail_effect_player")] public string TrailEffectPlayer;
        [Entry("cruise_charge_time")] public float CruiseChargeTime;
        [Entry("cruise_power_usage")] public float CruisePowerUsage;
        [Entry("character_start_sound")] public string CharacterStartSound;
        [Entry("character_loop_sound")] public string CharacterLoopSound;
        [Entry("character_pitch_range")] public Vector2 CharacterPitchRange;
        [Entry("rumble_sound")] public string RumbleSound;
        [Entry("rumble_atten_range")] public Vector2 RumbleAttenRange;
        [Entry("rumble_pitch_range")] public Vector2 RumblePitchRange;
        [Entry("engine_kill_sound")] public string EngineKillSound;
        [Entry("cruise_start_sound")] public string CruiseStartSound;
        [Entry("cruise_loop_sound")] public string CruiseLoopSound;
        [Entry("cruise_stop_sound")] public string CruiseStopSound;
        [Entry("cruise_disrupt_sound")] public string CruiseDisruptSound;
        [Entry("cruise_disrupt_effect")] public string CruiseDisruptEffect;
        [Entry("cruise_backfire_sound")] public string CruiseBackfireSound;
        [Entry("outside_cone_attenuation")] public float OutsideConeAttenuation;
        [Entry("inside_sound_cone")] public float InsideSoundCone;
        [Entry("outside_sound_cone")] public float OutsideSoundCone;

        //EXTENSION
        [Entry("cruise_speed")] public float CruiseSpeed;
    }
}
