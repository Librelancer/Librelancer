using LibreLancer.Ini;

namespace LibreLancer.Data.Effects;

public class Simple
{
    [Entry("nickname", Required = true)] public string Nickname;
    [Entry("DA_archetype")] public string DaArchetype;
    [Entry("material_library")] public string MaterialLibrary;
    [Entry("mass")] public float Mass;
}
