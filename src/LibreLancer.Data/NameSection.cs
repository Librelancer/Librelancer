using LibreLancer.Ini;

namespace LibreLancer.Data;

public class NameSection
{
    [Entry("name")] 
    public string Name;

    public override string ToString() => Name;
}