using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.MBases;

[ParsedSection]
public partial class BaseFaction
{
    [Entry("faction", Required = true)]
    public string Faction = null!;
    [Entry("weight")]
    public float Weight;
    [Entry("npc", Multiline = true)]
    public List<string> Npcs = [];

    public List<BaseFactionMission> Missions = [];

    //Unused, removed by JFLP
    [Entry("offers_missions", Presence = true)]
    public bool OffersMissions;

    [EntryHandler("mission_type", MinComponents = 3, Multiline = true)]
    private void HandleMissionType(Entry e)
    {
        var weight = e.Count > 3 ? e[3].ToSingle() : 100;
        Missions.Add(new BaseFactionMission(e[0].ToString(), e[1].ToSingle(), e[2].ToSingle(), weight));
    }
}

public record BaseFactionMission(string Type, float MinDiff, float MaxDiff, float Weight);
