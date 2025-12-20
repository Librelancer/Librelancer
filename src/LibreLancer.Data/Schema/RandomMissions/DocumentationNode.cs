using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.RandomMissions;

[ParsedSection]
public partial class DocumentationNode : VignetteNode
{
    [Entry("documentation")]
    public string Documentation;
}
