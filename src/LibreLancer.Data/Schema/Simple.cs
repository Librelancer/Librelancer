using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema;

[ParsedSection]
public partial class Simple
{
    [Entry("nickname", Required = true)]
    public string Nickname;
    [Entry("DA_archetype", Required = true)]
    public string DaArchetypeName;
    [Entry("material_library", Multiline = true)]
    public List<string> MaterialLibrary = new();
    [Entry("mass")]
    public float Mass;
    [Entry("LODranges")]
    public float[] LODranges;
}
