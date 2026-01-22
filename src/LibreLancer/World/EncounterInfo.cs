using System.Collections.Generic;
using LibreLancer.Data.GameData;
using LibreLancer.Data.Schema.Missions;

namespace LibreLancer.World;

public class EncounterInfo
{
    public FormationDef Formation;
    public List<EncounterEntry> Ships = new();
}

public record EncounterEntry(ObjectName Name, Voice? Voice, ShipArch Ship);
