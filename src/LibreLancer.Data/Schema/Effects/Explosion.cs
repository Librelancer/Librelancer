using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Effects;

[ParsedSection]
public partial class Explosion
{
    [Entry("nickname", Required = true)] public string Nickname;
    [Entry("lifetime")] public Vector2 Lifetime;
    [Entry("process")] public string Process;
    [Entry("num_child_pieces")] public int NumChildPieces;
    [Entry("innards_debris_start_time")] public float InnardsDebrisStartTime;
    [Entry("debris_impulse")] public float DebrisImpulse;

    public List<(string Name, float Weight)> Effects = new List<(string Name, float Weight)>();
    public List<(string Name, float Weight)> DebrisTypes = new List<(string Name, float Weight)>();

    [EntryHandler("effect", MinComponents = 2, Multiline = true)]

    void HandleEffect(Entry e)
    {
        Effects.Add((e[0].ToString(), e[1].ToSingle()));
    }

    [EntryHandler("debris_type", MinComponents = 2, Multiline = true)]
    void HandleDebrisType(Entry e)
    {
        DebrisTypes.Add((e[0].ToString(), e[1].ToSingle()));
    }
}
