namespace LibreLancer.Data.GameData.RandomMissions;

public class VignetteDecision(int id, string nickname) : VignetteTreeNode(id)
{
    public string Nickname { get; } = nickname;

    public override string ToString() => $"{Id}: Decision {Nickname}";
}
