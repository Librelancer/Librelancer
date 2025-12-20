using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Schema.RandomMissions;

namespace LibreLancer.Data.GameData.RandomMissions;

public class VignetteTree
{
    public Dictionary<int, VignetteAst> Nodes = new();
    public VignetteAst StartNode;
    private int _nextId = 3;

    static bool EmptyArray<T>(T[] array) => array == null || array.Length == 0;
    public static bool IsEmptyData(AstData dat) =>
        dat.Data.Difficulty == null &&
        EmptyArray(dat.Data.OfferGroup) &&
        EmptyArray(dat.Data.HostileGroup) &&
        EmptyArray(dat.Data.AllowableZoneTypes) &&
        dat.Data.CommSequences.Count == 0 &&
        dat.Data.OfferTexts.Count == 0 &&
        dat.Data.ObjectiveTexts.Count == 0 &&
        dat.Data.FailureText.Target == null &&
        dat.Data.RewardText.Target == null;

    public static VignetteTree FromIni(VignetteParamsIni vparams)
    {
        // Construct tree from VignetteParamsIni
        var tree = new VignetteTree();

        HashSet<int> unreferenced = new HashSet<int>();
        // Construct nodes
        int newId = 0;
        foreach (var n in vparams.Nodes) {
            if (n is DecisionNode dec)
            {
                tree.Nodes[n.NodeId] = new AstDecision(n.NodeId, dec);
            }
            else if (n is DataNode dat)
            {
                tree.Nodes[n.NodeId] = new AstData(n.NodeId, dat);
            }
            else if (n is DocumentationNode doc)
            {
                tree.Nodes[n.NodeId] = new AstDoc(n.NodeId, doc);
            }
            unreferenced.Add(n.NodeId);
            newId = Math.Max(newId, n.NodeId);
        }
        newId++;
        // Setup children
        foreach (var kv in tree.Nodes)
        {
            var src = vparams.Nodes.First(x => x.NodeId == kv.Key);
            kv.Value.Children = new List<VignetteAst>();
            for (int i = 0; i < src.ChildId.Count; i++)
            {
                var child = src.ChildId[i];
                if (!tree.Nodes.ContainsKey(child))
                {
                    throw new Exception($"Cannot find node {child}");
                }
                unreferenced.Remove(src.ChildId[i]);
                kv.Value.Children.Add(tree.Nodes[src.ChildId[i]]);
            }
        }

        // Get start node
        if (unreferenced.Count > 1)
            throw new Exception("More than one orphan start node");
        if (unreferenced.Count == 0)
            throw new Exception("No start node");
        tree.StartNode = tree.Nodes[unreferenced.First()];
        return tree;
    }


    public void Replace(VignetteAst oldNode, VignetteAst newNode)
    {
        foreach (var kv in Nodes)
        {
            for (int i = 0; i < kv.Value.Children.Count; i++)
            {
                if (kv.Value.Children[i] == oldNode)
                    kv.Value.Children[i] = newNode;
            }
        }

        Nodes.Remove(oldNode.Id);
        Nodes[newNode.Id] = newNode;
    }

    public int NextId() => _nextId++;

    public void FlattenEmptyNodes(VignetteAst startNode = null)
    {
        var optQueue = new Queue<int>();
        foreach (var k in Nodes.Where(x => x.Value != startNode)
                     .Select(x => x.Key))
            optQueue.Enqueue(k);
        // Flatten empty DataNodes
        while (optQueue.Count > 0)
        {
            var k = optQueue.Dequeue();
            if (!Nodes.TryGetValue(k, out var node))
                continue;
            if (node is AstData d &&
                IsEmptyData(d) &&
                d.Children.Count == 1)
            {
                Replace(node, node.Children[0]);
                optQueue.Enqueue(node.Children[0].Id);
            }
        }
    }

    public Dictionary<int, int> CullAndGetReferenceCount(VignetteAst startNode)
    {
        Dictionary<int, int> references = new Dictionary<int, int>();
        while (true)
        {
            references = new Dictionary<int, int>();
            foreach (var kv in Nodes)
            {
                // Collect references
                foreach (var c in kv.Value.Children)
                {
                    if (!references.ContainsKey(c.Id))
                        references.Add(c.Id, 1);
                    else
                        references[c.Id] = c.Id + 1;
                }
            }

            var empties = Nodes.Where(x =>
                x.Value != startNode && !references.ContainsKey(x.Value.Id)).ToArray();
            if (empties.Length == 0)
            {
                return references;
            }

            foreach (var e in empties)
            {
                Nodes.Remove(e.Key);
            }
        }
    }
}

public abstract class VignetteAst(int id)
{
    public int Id = id;
    public List<VignetteAst> Children = new List<VignetteAst>();
}

public enum DataNodeKind
{
    None,
    Difficulty,
    CommSequence,
    AllowableZone,
    Objective,
    Offer,
    Weight,
    Closed
}

public class AstData(int id, DataNode data) : VignetteAst(id)
{
    public DataNode Data = data;

    public DataNodeKind GetKind()
    {
        if (Children.Count > 0)
            return DataNodeKind.Closed;
        if (data.RewardText.Target != null ||
            data.FailureText.Target != null ||
            data.ObjectiveTexts.Count > 0)
            return DataNodeKind.Objective;
        if (data.Difficulty != null)
            return DataNodeKind.Difficulty;
        if (data.Weight != null)
            return DataNodeKind.Weight;
        if (data.CommSequences.Count > 0)
            return DataNodeKind.CommSequence;
        if (data.AllowableZoneTypes != null)
            return DataNodeKind.AllowableZone;
        if (data.OfferTexts.Count > 0)
            return DataNodeKind.Offer;
        return DataNodeKind.None;
    }

    public bool KindMatch(DataNodeKind a, DataNodeKind b)
    {
        var k = GetKind();
        return k == a || k == b;
    }

    public override string ToString() => $"{id}: DATA";
}

public class AstDoc(int id, DocumentationNode docs) : VignetteAst(id)
{
    public DocumentationNode Docs = docs;

    public override string ToString() => $"{id}: {Docs?.Documentation ?? "NULL"}";
}

public class AstDecision(int id, DecisionNode decision) : VignetteAst(id)
{
    public DecisionNode Decision = decision;

    public string[] GroupA;
    public string[] GroupB;

    public override string ToString() => $"{id}: {Decision?.Nickname}";
}

public class AstIfElse(int id) : VignetteAst(id)
{
    public List<string> Conditions = new List<string>();
}
