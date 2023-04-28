using System.Collections.Generic;
using LibreLancer.Ini;

namespace LibreLancer.Data;

public class BaseFaction : ICustomEntryHandler
{
    [Entry("faction")] 
    public string Faction;
    [Entry("weight")] 
    public float Weight;
    [Entry("npc", Multiline = true)] 
    public List<string> Npcs = new List<string>();

    public List<BaseFactionMission> Missions = new List<BaseFactionMission>();

    //Unused, removed by JFLP
    [Entry("offers_missions", Presence = true)] 
    public bool OffersMissions; 

    private static CustomEntry[] entries = {
        new("mission_type", (f, e) => ((BaseFaction) f).Missions.Add(
            new BaseFactionMission(e[0].ToString(), e[1].ToSingle(), e[2].ToSingle(), e[3].ToSingle())
        ))
    };
    
    IEnumerable<CustomEntry> ICustomEntryHandler.CustomEntries => entries;
}

public record BaseFactionMission(string Type, float MinDiff, float MaxDiff, float Weight);