using System;
using System.Collections;
using System.Collections.Generic;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.RandomMissions;
using LibreLancer.Data.Schema.RandomMissions;

namespace LibreLancer.Server.RandomMissions;

public record PossibleMission(VignetteData EndNode, List<MissionVariantPath> Paths);


public record MissionVariantPath(VignetteBranches Branches, VignetteDecisions Decisions, double Probability)
{
    public VignetteStrings GetStrings(VignetteTree tree, VC6Random random)
    {
        VignetteStrings vinfo = new();
        VignetteTreeNode? n = tree.StartNode;
        int i = 0;
        while (i < Branches.Count && n != null)
        {
            if (n is VignetteData data)
            {
                if (data.RewardText.Target != null)
                {
                    vinfo.RewardText = data.RewardText;
                }

                if (data.FailureText.Target != null)
                {
                    vinfo.FailureText = data.FailureText;
                }

                if (data.OfferTexts is { Count: > 0 })
                {
                    // Each entry is a complete alternative containing all of the
                    // fragments for this node. Select one entry, then retain every
                    // item in it (faction/target, location, and closing text).
                    var offerText = data.OfferTexts.Count == 1
                        ? data.OfferTexts[0]
                        : data.OfferTexts[random.Next() % data.OfferTexts.Count];
                    if (offerText.Op == OfferTextOp.replace)
                        vinfo.OfferText = [];
                    vinfo.OfferText.AddRange(offerText.Items);
                }

                foreach (var str in data.ObjectiveTexts)
                {
                    vinfo.ObjectiveStrings[str.Target!] = str;
                }

                foreach (var comm in data.CommSequences)
                {
                    vinfo.CommSequences[comm.Event] = comm;
                }
            }
            else if (n is VignetteDebug docs && !string.IsNullOrEmpty(docs.Message))
            {
                vinfo.Messages.Add(docs.Message);
            }
            n = Branches[i++] ? n.Left : n.Right;
        }
        return vinfo;
    }
}

public struct VignetteBranches : IEnumerable<bool>
{
    private BitArray128 data;
    private int count;
    public int Count => count;

    public bool this[int index] => data[index];

    public void Add(bool b)
    {
        data[count++] = b;
    }

    public void Pop()
    {
        count--;
    }

    public VignetteBranches CopyReversed()
    {
        var dest = new VignetteBranches();
        for (int i = 0; i < Count; i++)
        {
            dest.data[i] = data[Count - 1 - i];
        }
        dest.count = count;
        return dest;
    }

    public Enumerator GetEnumerator() => new Enumerator(this);

    IEnumerator<bool> IEnumerable<bool>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<bool>
    {
        private readonly VignetteBranches _branches;
        private int _index;

        internal Enumerator(VignetteBranches branches)
        {
            _branches = branches;
            _index = -1;
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _branches.Count;
        }

        public void Reset() => _index = -1;

        public bool Current => _branches[_index];

        object IEnumerator.Current => Current;

        public void Dispose() { }
    }
}

public record VignetteGraphParameters(Faction OfferGroup, Faction HostileGroup, float Difficulty, AllowedZoneType ZoneType);

public static class VignetteWalker
{
    private const int DepthMax = 100;

    /*
     * NOTE: These steps should be split out later to avoid excess work, when we have a better
     * API that both the game and VignetteTester can use.
     *
     * Freelancer's mission generation algorithm goes through vignetteparams.ini in 3 steps
     *
     * 1) Step through and find all the valid leaf nodes
     * 2) Select one valid leaf node randomly using the weights provided
     * 3) Backtrack from the leaf node to find all the valid paths to that node
     * 4) Calculate a "probability" weight, based on the amount of times there is a decision node.
     *    It doesn't matter if a decision node's children are culled/not accessible. It always halves probability
     * 5) Select a path randomly using the probability as the weight.
     *
     * Other notes: To achieve the same order, all the node's parents are stored on the node. They are sorted by ID.
     */
    public static IReadOnlyList<PossibleMission> Enumerate(VignetteTree tree, VignetteGraphParameters p)
    {
        var results = new List<PossibleMission>();

        List<VignetteData> endNodes = new();
        FindAccessibleLeafNodes(tree.StartNode, null, 0, endNodes, p);

        foreach (var endNode in endNodes)
        {
            var treePaths = new List<VignetteBranches>();
            VignetteBranches scratchSpace = new();
            FindPathsBackwards(
                currentNode: endNode,
                startNode: tree.StartNode,
                p: p,
                currentBranches: ref scratchSpace,
                paths: treePaths
            );

            if (treePaths.Count > 0)
            {
                List<MissionVariantPath> paths = new(treePaths.Count);
                foreach (var path in treePaths)
                {
                    double probability = 1.0f;
                    VignetteTreeNode? n = tree.StartNode;
                    VignetteDecisions decisions = new();
                    int i = 0;
                    while (i < path.Count && n != null)
                    {
                        if (n is VignetteDecision dec)
                        {
                            decisions.Set(dec.Nickname, path[i]);
                            probability *= 0.5f;
                        }
                        n = path[i++] ? n.Left : n.Right;
                    }
                    paths.Add(new(path, decisions, probability));
                }
                results.Add(new PossibleMission(endNode, paths));
            }
        }
        return results;
    }

    static void FindAccessibleLeafNodes(
        VignetteTreeNode node,
        VignetteTreeNode? src,
        int depth,
        List<VignetteData> endNodes,
        VignetteGraphParameters p)
    {
        if (depth > DepthMax)
        {
            FLLog.Error("VignetteParams", $"Tree walk exceeded depth max walking {src} -> {node}");
            return;
        }
        if (IsExcluded(node, p))
            return;
        switch (node)
        {
            case VignetteDebug:
                FindAccessibleLeafNodes(node.Left!, node, depth + 1, endNodes, p);
                break;
            case VignetteData d:
                if (node.Left == null && node.Right == null)
                {
                    if(!endNodes.Contains(d))
                        endNodes.Add(d);
                }
                else
                {
                    FindAccessibleLeafNodes(d.Left!, node, depth + 1, endNodes, p);
                }
                break;
            case VignetteDecision:
                if (node.Left != null)
                    FindAccessibleLeafNodes(node.Left!, node, depth + 1, endNodes, p);
                if(node.Right != null)
                    FindAccessibleLeafNodes(node.Right!, node, depth + 1, endNodes, p);
                break;
        }
    }

    private static void FindPathsBackwards(
        VignetteTreeNode currentNode,
        VignetteTreeNode startNode,
        VignetteGraphParameters p,
        ref VignetteBranches currentBranches,
        List<VignetteBranches> paths)
    {
        if (currentNode == startNode)
        {
            paths.Add(currentBranches.CopyReversed());
            return;
        }

        foreach (var parent in currentNode.Parents)
        {
            if (IsExcluded(parent, p))
                continue;

            currentBranches.Add(parent.Left == currentNode);

            FindPathsBackwards(parent, startNode, p, ref currentBranches, paths);

            currentBranches.Pop();
        }
    }

    static bool IsExcluded(VignetteTreeNode node, VignetteGraphParameters p)
    {
        if (node is VignetteData dn)
        {
            if (!dn.Implemented)
                return true;

            if ((dn.AllowedZone & p.ZoneType) != p.ZoneType)
                return true;

            if (p.Difficulty < dn.DifficultyRange.X ||
                p.Difficulty > dn.DifficultyRange.Y)
                return true;

            if (dn.OfferGroups != null &&
                !dn.OfferGroups.Contains(p.OfferGroup.CRC))
                return true;

            if (dn.HostileGroups != null &&
                !dn.HostileGroups.Contains(p.HostileGroup.CRC))
                return true;
        }
        return false;
    }
}
