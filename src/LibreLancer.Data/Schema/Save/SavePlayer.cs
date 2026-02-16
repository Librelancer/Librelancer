// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Text;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Save;

public class PlayerEquipment
{
    public HashValue Item;
    public string? Hardpoint;
    public float Unknown = 1; //Either health or count, not sure
    public PlayerEquipment() { }
    public PlayerEquipment(Entry e)
    {
        var s = e[0].ToString();
        Item = !uint.TryParse(s, out uint hash) ? new HashValue(s) : hash;
        if (e.Count < 2) return;
        //Extra
        Hardpoint = e[1].ToString();
        if (e.Count > 2) Unknown = e[2].ToSingle();
    }

    public string ToString(string ename)
    {
        return $"{ename} = {(uint)Item}, {Hardpoint}, {Unknown}";
    }

    public override string ToString() => ToString("equip");
}

public class PlayerCargo
{
    //hash, count, percentage_health, UNK, mission_cargo
    public HashValue Item;
    public float PercentageHealth = 1;
    public int Count;
    public bool IsMissionCargo;
    //Some unknowns here
    public PlayerCargo() { }
    public PlayerCargo(Entry e)
    {
        var s = e[0].ToString();
        if (!uint.TryParse(s, out uint hash)) Item = new HashValue(s);
        else Item = hash;
        Count = e[1].ToInt32();
        if (e.Count > 2)
            PercentageHealth = e[2].ToSingle();
        if (e.Count > 4)
            IsMissionCargo = e[4].ToBoolean();
    }

    public string ToString(string ename)
    {
        string hStr = "";
        if (PercentageHealth < 1) hStr = PercentageHealth.ToString(CultureInfo.InvariantCulture);
        return $"{ename} = {(uint)Item}, {Count}, {hStr}, , {(IsMissionCargo ? 1 : 0)}";
    }

    public override string ToString() => ToString("cargo");
}

public class LogEntry
{
    public int[] Data = [];

    public LogEntry() { }
    public LogEntry(Entry e) => Data = e.Select(x => x.ToInt32()).ToArray();
}

public struct VisitEntry
{
    public HashValue Obj;
    public int Visit;
    public VisitEntry(HashValue obj, int visit)
    {
        Obj = obj;
        Visit = visit;
    }

    public VisitEntry(Entry e) : this(new HashValue(e[0]), e[1].ToInt32()) { }
}

[ParsedSection]

public partial class SavePlayer : IWriteSection
{
    [Entry("descrip_strid")] public int DescripStrid;

    public string? Description;

    public DateTime? TimeStamp;

    public string? Name;
    [Entry("rank")] public int Rank;

    [Entry("money")] public long Money;

    [Entry("num_kills")] public int NumKills;
    [Entry("num_misn_successes")] public int NumMissionSuccesses;
    [Entry("num_misn_failures")] public int NumMissionFailures;

    public List<SaveRep> House = [];

    [Entry("voice")] public string? Voice;
    [Entry("costume")] public string? Costume;
    [Entry("com_costume")] public string? ComCostume;
    [Entry("com_body")] public HashValue ComBody;
    [Entry("com_head")] public HashValue ComHead;
    [Entry("com_lefthand")] public HashValue ComLeftHand;
    [Entry("com_righthand")] public HashValue ComRightHand;
    [Entry("body")] public HashValue Body;
    [Entry("head")] public HashValue Head;
    [Entry("lefthand")] public HashValue LeftHand;
    [Entry("righthand")] public HashValue RightHand;

    [Entry("system")] public string? System;
    [Entry("base")] public string? Base;
    [Entry("pos")] public Vector3 Position;
    [Entry("rotate")] public Vector3 Rotate;

    [Entry("location")] public int Location;

    [Entry("ship_archetype")] public HashValue ShipArchetype;

    public List<PlayerEquipment> Equip = [];
    public List<PlayerCargo> Cargo = [];
    public List<VisitEntry> Visit = [];
    public List<LogEntry> Log = [];

    [EntryHandler("tstamp", MinComponents = 2)]
    private void HandleTimestamp(Entry e) => TimeStamp =  DateTime.FromFileTime(e[0].ToInt64() << 32 | e[1].ToInt64());

    [EntryHandler("house", MinComponents = 2, Multiline = true)]
    private void HandleHouse(Entry e) => House.Add(new SaveRep(e));

    [EntryHandler("log", MinComponents = 2, Multiline = true)]
    private void HandleLog(Entry e) => Log.Add(new LogEntry(e));

    [EntryHandler("visit", MinComponents = 2, Multiline = true)]
    private void HandleVisit(Entry e) => Visit.Add(new VisitEntry(e));

    [EntryHandler("cargo", MinComponents = 2, Multiline = true)]
    private void HandleCargo(Entry e) => Cargo.Add(new PlayerCargo(e));

    [EntryHandler("equip", MinComponents = 1, Multiline = true)]
    private void HandleEquip(Entry e) => Equip.Add(new PlayerEquipment(e));

    [Entry("interface")] public int Interface;

    [EntryHandler("description")]
    private void HandleDescription(Entry e)
    {
        try
        {
            var bytes = e[0].ToString().SplitInGroups(2).Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray();
            Description = Encoding.BigEndianUnicode.GetString(bytes);
        }
        catch (Exception)
        {
            Description = string.Join(',', e.Select(x => x.ToString()));
        }
    }

    [EntryHandler("name")]
    private void HandleName(Entry e)
    {
        try
        {
            var bytes = e[0].ToString().SplitInGroups(2).Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray();
            Name = Encoding.BigEndianUnicode.GetString(bytes);
        }
        catch (Exception)
        {
            Name = string.Join(',', e.Select(x => x.ToString()));
        }
    }

    public static string? EncodeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var bytes = Encoding.BigEndianUnicode.GetBytes(name);
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("X2"));
        }

        return builder.ToString();
    }



    public void WriteTo(IniBuilder builder)
    {
        var fileTime = TimeStamp?.ToFileTime() ?? 0;

        var sec = builder.Section("Player")
            .OptionalEntry("descrip_strid", DescripStrid)
            .OptionalEntry("description", EncodeName(Description))
            .Entry("tstamp", (uint)(fileTime >> 32), (uint)(fileTime & 0xFFFFFFFF))
            .OptionalEntry("name", EncodeName(Name))
            .Entry("rank", Rank);

        foreach (var h in House)
        {
            sec.Entry("house", h.Reputation, h.Group!);
        }

        sec.Entry("money", Money)
            .Entry("num_kills", NumKills)
            .Entry("num_misn_successes", NumMissionSuccesses)
            .Entry("num_misn_failures", NumMissionFailures)
            .OptionalEntry("voice", Voice)
            .OptionalEntry("com_body", ComBody)
            .OptionalEntry("com_head", ComHead)
            .OptionalEntry("com_lefthand", ComLeftHand)
            .OptionalEntry("com_righthand", ComRightHand)
            .OptionalEntry("body", Body)
            .OptionalEntry("head", Head)
            .OptionalEntry("lefthand", LeftHand)
            .OptionalEntry("righthand", RightHand)
            .OptionalEntry("system", System)
            .OptionalEntry("base", Base);
        if (string.IsNullOrWhiteSpace(Base))
        {
            sec.Entry("pos", Position)
                .Entry("rot", Rotate);
        }
        sec.OptionalEntry("location", (uint)Location)
            .OptionalEntry("ship_archetype", ShipArchetype);
        foreach (var e in Equip)
            sec.Entry("equip", e.Item, (e.Hardpoint ?? ""), e.Unknown);
        foreach (var c in Cargo)
        {
            ValueBase hstr = c.PercentageHealth < 1
                ? new SingleValue(c.PercentageHealth, null)
                : "";
            sec.Entry("cargo", c.Item, c.Count, hstr, "", (c.IsMissionCargo ? 1 : 0));
        }

        foreach (var v in Visit)
            sec.Entry("visit", v.Obj, v.Visit);

        foreach (var l in Log)
            sec.Entry("log", l.Data.Select(x => (ValueBase)new Int32Value(x)).ToArray());

        sec.Entry("interface", Interface);
    }
}
