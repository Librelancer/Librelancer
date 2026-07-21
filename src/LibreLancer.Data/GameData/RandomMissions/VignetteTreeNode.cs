using System.Collections.Generic;

namespace LibreLancer.Data.GameData.RandomMissions;

public abstract class VignetteTreeNode(int id)
{
    public int Id = id;
    public VignetteTreeNode? Left;
    public VignetteTreeNode? Right;
    public List<VignetteTreeNode> Parents = [];
}
