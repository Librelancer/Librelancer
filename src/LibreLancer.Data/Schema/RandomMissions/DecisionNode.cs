using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.RandomMissions;

[ParsedSection]
public partial class DecisionNode : VignetteNode
{
    [Entry("nickname", Required = true)] public string Nickname = null!;
}
