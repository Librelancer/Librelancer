namespace LibreLancer.Data.GameData;

public class NewsItem
{
    public StoryIndex? From;
    public StoryIndex? To;
    public required string? Icon;
    public required string? Logo;
    public int Headline;
    public int Text;
    public bool AutoSelect = false;
    public string? Audio; // Unused: Vanilla

    public NewsItem Clone() => (NewsItem)MemberwiseClone();
}
