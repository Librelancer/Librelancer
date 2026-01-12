using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe;

[ParsedSection]
public partial class SystemMusic
{
    [Entry("space")]
    public string? Space;
    [Entry("danger")]
    public string? Danger;
    [Entry("battle")]
    public string? Battle;
}
