using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Effects;

[ParsedSection]
public partial class Explosion
{
    [Entry("nickname", Required = true)] public string Nickname = null!;
    [Entry("lifetime")] public Vector2 Lifetime;
    [Entry("process")] public string? Process;
    [Entry("num_child_pieces")] public int NumChildPieces;
    [Entry("debris_impulse")] public float DebrisImpulse;
    [Entry("innards_debris_start_time")] public float InnardsDebrisStartTime;
    [Entry("innards_debris_num")] public int InnardsDebrisNum;
    [Entry("innards_debris_radius")] public float InnardsDebrisRadius;
    [Entry("innards_debris_object", Multiline = true)]
    public List<string> InnardsDebrisObjects = [];

    public string? Effect;
    public float EffectSParam; // guess

    public List<(string Name, float Weight)> DebrisTypes = [];

    [EntryHandler("effect", MinComponents = 1)]
    private void HandleEffect(Entry e)
    {
        Effect = e[0].ToString();
        EffectSParam = e.Count > 1 ? e[1].ToSingle() : 0.0f;
    }

    [EntryHandler("debris_type", MinComponents = 2, Multiline = true)]
    private void HandleDebrisType(Entry e)
    {
        DebrisTypes.Add((e[0].ToString(), e[1].ToSingle()));
    }
}
