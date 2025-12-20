using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.RandomMissions;

[ParsedSection]
public partial class DecisionNode : VignetteNode
{
    [Entry("nickname")] public string Nickname;
}
