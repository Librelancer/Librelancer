using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe.Rooms;

[ParsedSection]
public partial class CharacterPlacement
{
    [Entry("name", Required = true)]
    public string Name = null!;
    [Entry("start_script")]
    public string? StartScript;
}
