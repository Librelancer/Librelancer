namespace LibreLancer.Data;

public abstract class NicknameItem
{
    public string Nickname;

    public override string ToString() => Nickname ?? "(null nickname)";
}
