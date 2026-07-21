using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LibreLancer.Data.Schema.RandomMissions;

namespace LibreLancer.Data.GameData.RandomMissions;

public class VignetteTree(VignetteTreeNode start)
{
    public VignetteTreeNode StartNode = start;

    static bool IsGroupExclusions([NotNullWhen(true)]string[]? nicknames)
    {
        if (nicknames == null || nicknames.Length == 0)
            return false;
        if (nicknames.Length == 1 && "all".Equals(nicknames[0], StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    public static VignetteTree FromIni(VignetteParamsIni vparams)
    {
        // Construct tree from VignetteParamsIni
        HashSet<int> unreferenced = [];
        Dictionary<int, VignetteTreeNode> nodesById = new();
        List<(VignetteNode Ini, VignetteTreeNode Runtime)> allNodes = new();
        // Construct nodes
        foreach (var n in vparams.Nodes)
        {
            VignetteTreeNode run = null!;
            if (n is DecisionNode dec)
            {
                run = new VignetteDecision(n.NodeId, dec.Nickname ?? "null");
            }
            else if (n is DataNode dat)
            {
                AllowedZoneType zone = AllowedZoneType.All;
                if (dat.AllowableZoneTypes is { Length: > 0 })
                {
                    zone = AllowedZoneType.None;
                    foreach (var d in dat.AllowableZoneTypes)
                    {
                        if (Enum.TryParse<AllowedZoneType>(d, true, out var z))
                        {
                            zone |= z;
                        }
                    }
                }
                run = new VignetteData(n.NodeId)
                {
                    OfferGroups = IsGroupExclusions(dat.OfferGroup) ? new(dat.OfferGroup.Select(FLHash.CreateID)) : null,
                    HostileGroups = IsGroupExclusions(dat.HostileGroup) ? new(dat.HostileGroup.Select(FLHash.CreateID)) : null,
                    Weight = dat.Weight ?? 1,
                    AllowedZone = zone,
                    DifficultyRange = dat.Difficulty ?? new(-1000, 1000),
                    Implemented = dat.Implemented,
                    FailureText = dat.FailureText,
                    RewardText = dat.RewardText,
                    ObjectiveTexts = dat.ObjectiveTexts,
                    OfferTexts = dat.OfferTexts,
                    CommSequences = dat.CommSequences
                };
            }
            else if (n is DocumentationNode doc)
            {
                run = new VignetteDebug(n.NodeId, doc.Documentation ?? doc.NodeId.ToString());
            }
            else
            {
                throw new NotImplementedException(); // unreachable
            }
            nodesById[n.NodeId] = run;
            unreferenced.Add(n.NodeId);
            allNodes.Add((n, run));
        }

        foreach (var pair in allNodes)
        {
            if (pair.Ini.ChildId is { Count: > 0 })
            {
                pair.Runtime.Left = nodesById[pair.Ini.ChildId[0]];
                unreferenced.Remove(pair.Ini.ChildId[0]);
            }
            if (pair.Ini.ChildId is { Count: > 1 })
            {
                pair.Runtime.Right = nodesById[pair.Ini.ChildId[1]];
                unreferenced.Remove(pair.Ini.ChildId[1]);
            }
        }

        // Get start node
        if (unreferenced.Count > 1)
            throw new Exception("More than one orphan start node");
        if (unreferenced.Count == 0)
            throw new Exception("No start node");

        return new(nodesById[unreferenced.First()]);
    }
}
