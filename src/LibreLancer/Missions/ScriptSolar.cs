using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.Schema.Missions;

namespace LibreLancer.Missions;

public class ScriptSolar : NicknameItem
{
    public Archetype Archetype;
    public int IdsName;
    public Faction Faction;
    public string System;
    public Vector3 Position;
    public Quaternion Orientation = Quaternion.Identity;
    public string Base;
    public List<string> Labels = new List<string>();
    public float Radius;
    public string Voice;
    public CostumeEntry Costume = new();
    public string Loadout;
    public string Visit;
    public string Pilot;

    public static ScriptSolar FromIni(MissionSolar solar, GameItemDb db) =>
        new ScriptSolar()
        {
            Nickname = solar.Nickname,
            Archetype = db.Archetypes.Get(solar.Archetype),
            IdsName = solar.StringId,
            Faction = db.Factions.Get(solar.Faction),
            System =  solar.System,
            Position =  solar.Position,
            Orientation = solar.Orientation,
            Base = solar.Base,
            Labels = new(solar.Labels),
            Radius = solar.Radius,
            Voice = solar.Voice,
            Costume = new CostumeEntry(solar.Costume, db),
            Loadout = solar.Loadout,
            Pilot = solar.Pilot,
            Visit = solar.Visit
        };
}
