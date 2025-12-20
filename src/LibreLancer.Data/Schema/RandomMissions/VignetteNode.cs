using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.RandomMissions;

public abstract class VignetteNode
{
    [Entry("node_id", Required = true)] public int NodeId;
    [Entry("child_node", Multiline = true)] public List<int> ChildId = new List<int>();
}
