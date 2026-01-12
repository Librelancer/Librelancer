using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe.Rooms;

[ParsedSection]
public partial class PlayerShipPlacement
{
    [Entry("name", Required = true)]
    public string Name = null!;
    [Entry("launching_script")]
    public string? LaunchingScript;
    [Entry("landing_script")]
    public string? LandingScript;
}
