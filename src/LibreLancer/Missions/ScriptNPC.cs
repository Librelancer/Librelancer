using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.Schema.Missions;

namespace LibreLancer.Missions;

public class ScriptNPC : NicknameItem
{
    public CostumeEntry SpaceCostume = new();
    public Faction Affiliation;
    public string NpcShipArch;
    public int IndividualName;
    public string Voice;

    public static ScriptNPC FromIni(MissionNPC npc, GameItemDb db) => new()
    {
        Nickname = npc.Nickname,
        SpaceCostume = new CostumeEntry(npc.SpaceCostume, db),
        Affiliation = db.Factions.Get(npc.Affiliation),
        NpcShipArch = npc.NpcShipArch,
        IndividualName = npc.IndividualName,
        Voice = npc.Voice
    };
}

