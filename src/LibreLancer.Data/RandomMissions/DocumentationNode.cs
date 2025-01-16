using LibreLancer.Ini;

namespace LibreLancer.Data.RandomMissions;

public class DocumentationNode : VignetteNode
{
    [Entry("documentation")]
    public string Documentation;
}
