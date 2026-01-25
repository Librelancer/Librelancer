// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.Ini;

// ReSharper disable InconsistentNaming
namespace LibreLancer.Data.Schema.Missions;

[ParsedSection]
public partial class NNObjective
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;

    [Entry("state")]
    public string State = "HIDDEN";

    public NNObjectiveType Type;
    public string System = "";
    public int NameIds;
    public int ExplanationIds;
    public Vector3 Position;
    public string SolarNickname = "";

    [EntryHandler("type", MinComponents = 2)]
    void HandleType(Entry e)
    {
        if (!Enum.TryParse(e[0].ToString(), true, out Type))
        {
            IniDiagnostic.InvalidEnum(e, e.Section);
        }
        switch (Type)
        {
            case NNObjectiveType.ids:
                NameIds = e[1].ToInt32();
                break;
            case NNObjectiveType.navmarker:
                if (e.Count < 7)
                {
                    IniDiagnostic.Warn($"navmarker needs 7 entries, got {e.Count}", e);
                    return;
                }
                System = e[1].ToString();
                NameIds = e[2].ToInt32();
                ExplanationIds = e[3].ToInt32();
                Position = new(e[4].ToSingle(), e[5].ToSingle(), e[6].ToSingle());
                break;
            case NNObjectiveType.rep_inst:
                if (e.Count < 8)
                {
                    IniDiagnostic.Warn($"navmarker needs 8 entries, got {e.Count}", e);
                    return;
                }
                System = e[1].ToString();
                NameIds = e[2].ToInt32();
                ExplanationIds = e[3].ToInt32();
                Position = new(e[4].ToSingle(), e[5].ToSingle(), e[6].ToSingle());
                SolarNickname = e[7].ToString();
                break;
        }
    }
}

public enum NNObjectiveType
{
    ids,
    rep_inst,
    navmarker
}

