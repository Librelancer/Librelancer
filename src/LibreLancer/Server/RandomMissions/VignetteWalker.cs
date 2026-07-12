using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.RandomMissions;
using LibreLancer.Data.Schema.RandomMissions;

namespace LibreLancer.Server.RandomMissions;

public record PossibleMission(VignetteData EndNode, List<MissionVariantPath> Paths);

public record MissionVariantPath(List<VignetteTreeNode> Nodes, VignetteDecisions Decisions, double Probability)
{
    public VignetteStrings GetStrings(VC6Random r)
    {
        VignetteStrings vinfo = new();
        foreach (var n in Nodes)
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
                    var ot = data.OfferTexts.Count == 1
                        ? data.OfferTexts[0]
                        : data.OfferTexts[r.Next() % data.OfferTexts.Count];
                    if (ot.Op == OfferTextOp.replace)
                        vinfo.OfferText = [];
                    vinfo.OfferText.AddRange(ot.Items);
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
        }

        return vinfo;
    }
}

public record VignetteGraphParameters(
    Faction OfferGroup,
    Faction HostileGroup,
    float Difficulty,
    AllowedZoneType ZoneType);

public static class VignetteWalker
{
    public static IReadOnlyList<PossibleMission> Enumerate(VignetteTree tree, VignetteGraphParameters p)
    {
        var results = new List<PossibleMission>();

        Traverse(
            tree.StartNode,
            [],
            new(),
            1.0,
            results,
            p);

        return results;
    }

    static void Traverse(
        VignetteTreeNode node,
        List<VignetteTreeNode> visited,
        VignetteDecisions decisions,
        double probability,
        List<PossibleMission> output,
        VignetteGraphParameters p)
    {
        if (IsExcluded(node, p))
            return;

        visited.Add(node);

        var left = node.Left != null && !IsExcluded(node.Left, p) ? node.Left : null;
        var right = node.Right != null && !IsExcluded(node.Right, p) ? node.Right : null;
        var defPath = left ?? right;


        switch (node)
        {
            case VignetteDebug:
            {
                if (defPath == null)
                    return;

                Traverse(
                   defPath,
                    new List<VignetteTreeNode>(visited),
                    decisions,
                    probability,
                    output,
                    p);
                return;
            }

            case VignetteData data:
            {
                if (defPath == null)
                {
                    var leaf = output.FirstOrDefault(x => ReferenceEquals(x.EndNode, data));
                    if (leaf == null)
                    {
                        leaf = new(data, []);
                        output.Add(leaf);
                    }

                    leaf.Paths.Add(new MissionVariantPath(new(visited), decisions, probability));
                    return;
                }

                Traverse(
                    defPath,
                    new List<VignetteTreeNode>(visited),
                    decisions,
                    probability,
                    output,
                    p);
                return;
            }

            case VignetteDecision decision:
            {
                if (defPath == null)
                    return;

                if (left != null && right != null)
                {
                    Traverse(
                        left,
                        new List<VignetteTreeNode>(visited),
                        decisions.With(decision.Nickname, true),
                        probability * 0.5,
                        output,
                        p);

                    Traverse(
                        right,
                        new List<VignetteTreeNode>(visited),
                        decisions.With(decision.Nickname, false),
                        probability * 0.5,
                        output,
                        p);
                }
                else
                {
                    Traverse(
                        defPath,
                        new List<VignetteTreeNode>(visited),
                        decisions.With(decision.Nickname, true),
                        probability,
                        output,
                        p);
                }
                return;
            }

            default:
                throw new InvalidOperationException(); // unreachable
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
