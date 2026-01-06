using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Storyline;

[ParsedSection]
public partial class StoryMission
{
    [Entry("nickname", Required = true)] public string Nickname = null!;
    [Entry("file", Required = true)] public string File = null!;
}
