// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Pilots;

[ParsedSection]
public partial class EvadeDodgeBlock : PilotBlock
{
    [Entry("evade_dodge_cone_angle")] public float DodgeConeAngle;
    [Entry("evade_dodge_interval_time")] public float DodgeIntervalTime;
    [Entry("evade_dodge_time")] public float DodgeTime;
    [Entry("evade_dodge_distance")] public float DodgeDistance;
    [Entry("evade_activate_range")] public float ActivateRange;
    [Entry("evade_dodge_roll_angle")] public float RollAngle;
    [Entry("evade_dodge_waggle_axis_cone_angle")]
    public float DodgeWaggleAxisConeAngle;
    [Entry("evade_dodge_slide_throttle")]
    public float DodgeSlideThrottle;
    [Entry("evade_dodge_turn_throttle")] public float DodgeTurnThrottle;
    [Entry("evade_dodge_corkscrew_turn_throttle")]
    public float DodgeCorkscrewTurnThrottle;
    [Entry("evade_dodge_corkscrew_roll_throttle")]
    public float DodgeCorkscrewRollThrottle;
    [Entry("evade_dodge_corkscrew_roll_flip_direction")]
    public bool DodgeCorkscrewRollFlipDirection;
    [Entry("evade_dodge_interval_time_variance_percent")]
    public float DodgeIntervalTimeVariancePercent;
    [Entry("evade_dodge_cone_angle_variance_percent")]
    public float DodgeConeAngleVariancePercent;
    public List<DodgeStyle> DodgeStyleWeights = [];
    public List<DirectionWeight> DodgeDirectionWeights = [];

    [EntryHandler("evade_dodge_style_weight", MinComponents = 2, Multiline = true)]
    private void HandleDodgeStyle(Entry e) => DodgeStyleWeights.Add(new DodgeStyle(e));

    [EntryHandler("evade_dodge_direction_weight", MinComponents = 2, Multiline = true)]
    private void HandleDirectionWeight(Entry e) => DodgeDirectionWeights.Add(new DirectionWeight(e));

}

public class DodgeStyle
{
    public string? Style;
    public float Weight;

    public DodgeStyle()
    {
    }

    public DodgeStyle(Entry e)
    {
        Style = e[0].ToString();
        Weight = e[1].ToSingle();
    }
}
