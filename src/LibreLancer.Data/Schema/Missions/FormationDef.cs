using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Missions;

[ParsedSection]
public partial class FormationDef
{
    [Entry("nickname", Required = true)] public string Nickname = null!;
    [Entry("pos", Multiline = true)] public List<Vector3> Positions = [];
    [Entry("pl_pos")] public Vector3? PlayerPosition;
}
