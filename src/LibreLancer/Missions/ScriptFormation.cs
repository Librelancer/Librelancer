using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Data.Schema.Missions;

namespace LibreLancer.Missions;

public class ScriptFormation : NicknameItem
{
    public Vector3 Position = Vector3.Zero;
    public Quaternion Orientation = Quaternion.Identity;
    public string Formation;
    public List<ScriptShip> Ships = new();
    public MissionRelativePosition RelativePosition;

    public static ScriptFormation FromIni(
        MissionFormation formation,
        GameItemDb db,
        Dictionary<string, ScriptShip> ships)
    {
        var fm = new ScriptFormation()
        {
            Nickname = formation.Nickname,
            Position = formation.Position,
            Orientation = formation.Orientation,
            Formation = formation.Formation,
            RelativePosition = formation.RelativePosition
        };
        foreach (var s in formation.Ships)
        {
            if (ships.TryGetValue(s, out var ship))
                fm.Ships.Add(ship);
        }
        return fm;
    }
}
