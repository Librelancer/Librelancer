namespace LibreLancer.Data;

public abstract class NicknameItem
{
    public string Nickname = null!;
    public override string? ToString() => Nickname ?? "(null nickname)";
}
