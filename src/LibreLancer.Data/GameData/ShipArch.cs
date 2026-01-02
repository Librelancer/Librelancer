using System.Collections.Generic;
using LibreLancer.Data.Schema.Missions;

namespace LibreLancer.Data.GameData;

public class ShipArch : IdentifiableItem
{
    public Ship? Ship;
    public string? Loadout;
    public string? Pilot;
    public string? StateGraph;
    public int Level;
    public List<string> NpcClass = [];

    public static ShipArch FromIni(
        NPCShipArch ini,
        GameItemDb db) => new()
    {
        Nickname = ini.Nickname,
        CRC = FLHash.CreateID(ini.Nickname),
        Loadout = ini.Loadout,
        Ship = db.Ships.Get(ini.ShipArchetype),
        Pilot = ini.Pilot,
        StateGraph = ini.StateGraph,
        Level = ini.Level,
        NpcClass = ini.NpcClass != null ? new(ini.NpcClass) : []
    };
}
