namespace LibreLancer.Data;

public abstract class IdentifiableItem
{
    public string Nickname;
    public uint CRC;

    public override string ToString() => Nickname ?? "(null nickname)";
}
