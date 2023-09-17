using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using LibreLancer.Ini;

namespace LibreLancer.Data.Missions;

public class FormationDef
{
    [Entry("nickname", Required = true)] public string Nickname;
    [Entry("pos", Multiline = true)] public List<Vector3> Positions = new List<Vector3>();
    [Entry("pl_pos")] public Vector3? PlayerPosition;
}
