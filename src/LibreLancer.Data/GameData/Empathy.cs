namespace LibreLancer.Data.GameData;

public class Empathy
{
    public Faction? Other;
    public float Multiplier;

    public Empathy()
    {
    }

    public Empathy(Faction other, float mult)
    {
        Other = other;
        Multiplier = mult;
    }

    public override string ToString()
    {
        return $"{Other?.Nickname}: {Multiplier}";
    }
}
