using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Solar;

[ParsedSection]
public partial class DynamicAsteroid
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;
    [Entry("DA_archetype")]
    public string? DaArchetype;
    [Entry("material_library")]
    public string? MaterialLibrary;
    [Entry("explosion_arch")]
    public string? ExplosionArch;
}
