using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Missions;

[ParsedSection]
public partial class EncounterFormation
{
    public EncounterArrival? Arrival;
    [Entry("behavior")]
    public EncounterBehavior Behavior;
    [Entry("formation_by_class")]
    public string? FormationByClass;
    [Entry("zone_creation_distance")]
    public float ZoneCreationDistance;
    [Entry("allow_simultaneous_creation")]
    public bool AllowSimultaneousCreation;
    [Entry("times_to_create")]
    public string? TimesToCreate; //Unused, usually "infinite". Can be integer also

    public List<EncounterShipDefinition> Ships = new();

    [EntryHandler("ship_by_class", Multiline = true, MinComponents = 3)]
    void HandleShipByClass(Entry e)
    {
        Ships.Add(new EncounterShipDefinition(EncounterShipKind.Class, e[0].ToInt32(), e[1].ToInt32(), e[2].ToString()));
    }

    [EntryHandler("ship_by_npc_arch", Multiline = true, MinComponents = 3)]
    void HandleShipByNpcArch(Entry e)
    {
        Ships.Add(new EncounterShipDefinition(EncounterShipKind.NPCArch, e[0].ToInt32(), e[1].ToInt32(), e[2].ToString()));
    }

    [EntryHandler("arrival", MinComponents = 1)]
    void HandleArrival(Entry e)
    {
        Arrival = new EncounterArrival();
        foreach (var str in e.Select(x => x.ToString()))
        {
            if (str[0] == '-' && Enum.TryParse<Arrivals>(str.AsSpan(1), out var exclude))
                Arrival.Excludes.Add(exclude);
            else if(Enum.TryParse<Arrivals>(str, true, out var inc))
                Arrival.Includes.Add(inc);
            else
                IniDiagnostic.InvalidEnum(e, e.Section);
        }
    }


    [EntryHandler("pilot_job", Multiline = true, MinComponents = 1)]
    void HandlePilotJob(Entry e)
    {
        if (Ships.Count == 0)
            FLLog.Warning("Ini", "pilot_job without ship component in encounter");
        else
            Ships[^1].PilotJob = e[0].ToString();
    }

    [EntryHandler("make_class", Multiline = true, MinComponents = 1)]
    void HandleMakeClass(Entry e)
    {
        if (Ships.Count == 0)
            FLLog.Warning("Ini", "make_class without ship component in encounter");
        else
            Ships[^1].MakeClass = e[0].ToString();
    }
}

public class EncounterArrival
{
    public List<Arrivals> Includes = new();
    public List<Arrivals> Excludes = new();
}

public enum Arrivals
{
    all,
    object_all,
    tradelane,
    object_docking_ring,
    object_jump_gate,
    object_station,
    object_capital,
    cruise,
    buzz
}

public class EncounterShipDefinition(EncounterShipKind kind, int min, int max, string archetype)
{
    public EncounterShipKind Kind = kind;
    public string Archetype = archetype;
    public int Min = min;
    public int Max = max;
    public string? PilotJob;
    public string? MakeClass;
}

public enum EncounterShipKind
{
    Class,
    NPCArch
}


public enum EncounterBehavior
{
    wander,
    trade,
    patrol_path
}
