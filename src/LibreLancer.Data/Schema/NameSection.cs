using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema;

[ParsedSection]
public partial class NameSection
{
    [Entry("name")]
    public string Name;

    public override string ToString() => Name;
}
