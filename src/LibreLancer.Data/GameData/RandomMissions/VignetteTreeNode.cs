namespace LibreLancer.Data.GameData.RandomMissions;

public abstract class VignetteTreeNode(int id)
{
    public int Id = id;
    public VignetteTreeNode? Left;
    public VignetteTreeNode? Right;
}
