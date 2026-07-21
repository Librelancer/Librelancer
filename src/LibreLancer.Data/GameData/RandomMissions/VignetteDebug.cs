namespace LibreLancer.Data.GameData.RandomMissions;

public class VignetteDebug(int id, string message) : VignetteTreeNode(id)
{
    public string Message { get; } = message;

    public override string ToString() => $"{id} Debug: {Message}";
}
