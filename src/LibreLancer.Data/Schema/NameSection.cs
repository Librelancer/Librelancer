using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema;

[ParsedSection]
public partial class NameSection
{
    [Entry("name", Required = true)]
    public string Name = null!;

    public override string ToString() => Name;
}
