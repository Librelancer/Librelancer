using LibreLancer.Ini;

namespace LibreLancer.Data.Storyline;

public class StoryMission
{
    [Entry("nickname", Required = true)] public string Nickname;
    [Entry("file", Required = true)] public string File;

}
