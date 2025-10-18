namespace LibreLancer.ImUI;

public class DropdownOption
{
    public string Name;
    public char Icon;
    public object Tag;

    public DropdownOption(string name, char icon)
    {
        Name = name;
        Icon = icon;
    }
    public DropdownOption(string name, char icon, object tag)
    {
        Name = name;
        Icon = icon;
        Tag = tag;
    }
}
