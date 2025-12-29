using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Data.Schema.Missions;

namespace LibreLancer.Missions;

public class ScriptShip : NicknameItem
{
    public string System;
    public ScriptNPC NPC;
    public List<string> Labels = new List<string>(); //Multiple labels?
    public Vector3 Position;
    public Quaternion Orientation = Quaternion.Identity;
    public bool RandomName;
    public float Radius;
    public bool Jumper;

    public ArrivalObj ArrivalObj = new("", 0);
    public string InitObjectives;
    public MissionRelativePosition RelativePosition;
    public List<MissionShipCargo> Cargo = new List<MissionShipCargo>();

    public static ScriptShip FromIni(
        MissionShip ship,
        GameItemDb db,
        Dictionary<string, ScriptNPC> npcs)
    {
        var x = new ScriptShip()
        {
            Nickname = ship.Nickname,
            System = ship.System,
            Labels = new(ship.Labels),
            Position = ship.Position,
            Orientation = ship.Orientation,
            RandomName = ship.RandomName,
            Radius = ship.Radius,
            Jumper = ship.Jumper,
            ArrivalObj = ship.ArrivalObj,
            InitObjectives = ship.InitObjectives,
            RelativePosition = ship.RelativePosition,
            Cargo = new(ship.Cargo)
        };
        npcs.TryGetValue(ship.NPC, out x.NPC);
        return x;
    }

}
