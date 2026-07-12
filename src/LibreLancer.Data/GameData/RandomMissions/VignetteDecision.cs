namespace LibreLancer.Data.GameData.RandomMissions;

public class VignetteDecision(string nickname) : VignetteTreeNode
{
    public string Nickname { get; } = nickname;
}
