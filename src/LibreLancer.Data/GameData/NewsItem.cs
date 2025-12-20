namespace LibreLancer.Data.GameData;

public class NewsItem
{
    public StoryIndex From;
    public StoryIndex To;
    public string Icon;
    public string Logo;
    public int Headline;
    public int Text;
    public bool Autoselect;
    public string Audio; // Unused: Vanilla

    public NewsItem Clone() => (NewsItem)MemberwiseClone();
}
