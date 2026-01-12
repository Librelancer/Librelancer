// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Missions;

[ParsedSection]
public partial class MissionSolar
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;
    [Entry("faction")]
    public string? Faction;
    [Entry("system")]
    public string? System;
    [Entry("position")]
    public Vector3 Position;
    [Entry("orientation")]
    public Quaternion Orientation;
    [Entry("archetype")]
    public string? Archetype;
    [Entry("base")]
    public string? Base;
    [Entry("label", Multiline = true)]
    public List<string> Labels = [];
    [Entry("radius")]
    public float Radius;
    [Entry("voice")]
    public string? Voice;
    [Entry("costume")]
    public string[]? Costume;
    [Entry("loadout")]
    public string? Loadout;
    [Entry("string_id")]
    public int StringId;
    [Entry("pilot")]
    public string? Pilot;
    [Entry("visit")]
    public string? Visit;
}
