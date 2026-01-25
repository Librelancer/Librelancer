// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Missions;

[ParsedSection]
public partial class MissionShip
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;
    [Entry("system")]
    public string? System;
    [Entry("npc")]
    public string? NPC;
    [Entry("label", Multiline = true)]
    public List<string> Labels = []; //Multiple labels?
    [Entry("position")]
    public Vector3 Position;
    [Entry("orientation")]
    public Quaternion Orientation = Quaternion.Identity;
    [Entry("random_name")]
    public bool RandomName;
    [Entry("radius")]
    public float Radius;
    [Entry("jumper")]
    public bool Jumper;

    public ArrivalObj ArrivalObj = new("", 0);
    [Entry("init_objectives")]
    public string? InitObjectives;
    public List<MissionShipCargo> Cargo = [];

    [EntryHandler("cargo", Multiline = true, MinComponents = 2)]
    private void ParseCargo(Entry e) => Cargo.Add(new MissionShipCargo()
    {
        Cargo = e[0].ToString(),
        Count = e[1].ToInt32()
    });

    [EntryHandler("arrival_obj", MinComponents = 1)]
    private void ParseArrivalObj(Entry e)
    {
        int index = 0;
        if (e.Count > 1)
        {
            index = e[1].ToInt32();
        }

        ArrivalObj = new(e[0].ToString(), index);
    }

    public MissionRelativePosition RelativePosition = new();

    [EntryHandler("rel_pos", MinComponents = 3)]
    private void ParseRelativePosition(Entry entry) => RelativePosition = MissionRelativePosition.FromEntry(entry);
}

// Must be a class for editor purposes
public class MissionShipCargo
{
    public string? Cargo;
    public int Count;

    public MissionShipCargo Clone() => (MissionShipCargo)MemberwiseClone();
}

public struct ArrivalObj(string obj, int index)
{
    public string Object = obj;
    public int Index = index;
}
