using System.Collections.Generic;
using LibreLancer.Data.GameData;
using LibreLancer.Data.Schema.Missions;

namespace LibreLancer.World;

public class EncounterInfo
{
    public FormationDef? Formation;
    public EncounterFormation? FormationDefinition;
    public List<EncounterEntry> Ships = [];
}

public record EncounterEntry(ObjectName Name, Voice? Voice, ShipArch Ship, string? MakeClass);
