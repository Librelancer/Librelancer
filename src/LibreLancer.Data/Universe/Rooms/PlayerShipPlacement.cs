using LibreLancer.Ini;

namespace LibreLancer.Data.Universe.Rooms;

public class PlayerShipPlacement
{
    [Entry("name")] 
    public string Name;
    [Entry("launching_script")] 
    public string LaunchingScript;
    [Entry("landing_script")] 
    public string LandingScript;
}