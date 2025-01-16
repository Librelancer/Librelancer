using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;

namespace LibreLancer.Data.RandomMissions;

public class VignetteParamsIni : IniFile
{
    [Section("DataNode", typeof(DataNode))]
    [Section("DocumentationNode", typeof(DocumentationNode))]
    [Section("DecisionNode", typeof(DecisionNode))]
    public List<VignetteNode> Nodes = new List<VignetteNode>();

    public void AddFile(string path, FileSystem vfs) => ParseAndFill(path, vfs);
}
