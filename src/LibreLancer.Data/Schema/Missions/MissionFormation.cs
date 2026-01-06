// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Missions;

[ParsedSection]
public partial class MissionFormation
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;
    [Entry("position")]
    public Vector3 Position;
    [Entry("orientation")]
    public Quaternion Orientation;
    [Entry("formation")]
    public string? Formation;
    [Entry("ship", Multiline = true)]
    public List<string> Ships = [];


    public MissionRelativePosition RelativePosition = new();

    [EntryHandler("rel_pos", MinComponents = 3)]
    private void ParseRelativePosition(Entry entry) => RelativePosition = MissionRelativePosition.FromEntry(entry);
}
