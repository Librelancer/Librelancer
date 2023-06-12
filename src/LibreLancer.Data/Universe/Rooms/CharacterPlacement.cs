using LibreLancer.Ini;

namespace LibreLancer.Data.Universe.Rooms;

public class CharacterPlacement
{
    [Entry("name")] 
    public string Name;
    [Entry("start_script")] 
    public string StartScript;
}