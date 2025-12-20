using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe;

[ParsedSection]
public partial class SystemAmbient
{
    [Entry("color")]
    public Color3f Color;
}
