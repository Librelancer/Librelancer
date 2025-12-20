using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.GameData.RandomMissions;
using LibreLancer.Data.Schema.RandomMissions;

namespace LibreLancer.ContentEdit.RandomMissions;

public static class VignetteParamsDecompiler
{
    static bool IsEmptyError(VignetteAst ast)
    {
        if (ast is not AstData dat)
            return false;
        return !dat.Data.Implemented &&
               dat.Children.Count == 0 &&
               VignetteTree.IsEmptyData(dat);
    }


    static string FormatGroup(string[] g) => string.Join(", ", g);

    static void WriteNode(VignetteAst node,
        TabbedWriter writer,
        bool branchParent,
        Dictionary<int, int> references,
        Dictionary<string, string> groups,
        bool implement)
    {
        if (IsEmptyError(node))
        {
            writer.AppendLine("err_unimplemented;");
            return;
        }
        if (!implement && references[node.Id] > 1)
        {
            writer.AppendLine($"call node_{node.Id};");
            return;
        }

        if (node is AstIfElse ifElse)
        {
            for (int i = 0; i < ifElse.Conditions.Count; i++)
            {
                if (i == 0) writer.AppendLine($"if {ifElse.Conditions[i]}");
                else writer.AppendLine($"elif {ifElse.Conditions[i]}");
                writer.Indent();
                WriteNode(ifElse.Children[i], writer, ifElse.Conditions[i].StartsWith("group("), references, groups, false);
                writer.UnIndent();
            }
            if (ifElse.Conditions.Count < ifElse.Children.Count)
            {
                writer.AppendLine("else");
                writer.Indent();
                WriteNode(ifElse.Children[^1], writer, false, references, groups, false);
                writer.UnIndent();
            }
            writer.AppendLine("end");
        }
        else if (node is AstDecision dec)
        {
            var condA = dec.GroupA != null ? $"group ({groups[FormatGroup(dec.GroupA)]})" : dec.Decision.Nickname;
            var b = dec.GroupB != null ? $"elif group({groups[FormatGroup(dec.GroupB)]})" : "else";

            writer.AppendLine($"if {condA}");
            writer.Indent();
            WriteNode(dec.Children[0], writer, dec.GroupA != null, references, groups, false);
            writer.UnIndent();
            writer.AppendLine(b);
            writer.Indent();
            WriteNode(dec.Children[^1], writer, dec.GroupB != null, references, groups, false);
            writer.UnIndent();
            writer.AppendLine("end");
        }
        else if (node is AstDoc doc)
        {
            writer.AppendLine($"doc {doc.Docs.Documentation};");
            foreach (var c in node.Children)
            {
                WriteNode(c, writer, false, references, groups, false);
            }
        }
        else if (node is AstData data)
        {
            if (!data.Data.Implemented)
                writer.AppendLine("err_unimplemented;");
            if (!branchParent && data.Data.OfferGroup?.Length > 0)
                writer.AppendLine($"offer_group {groups[FormatGroup(data.Data.OfferGroup)]};");
            if(data.Data.HostileGroup?.Length > 0)
                writer.AppendLine($"hostile_group {groups[FormatGroup(data.Data.HostileGroup)]};");
            if (data.Data.AllowableZoneTypes?.Length > 0)
                writer.AppendLine($"allowable_zone_types {FormatGroup(data.Data.AllowableZoneTypes)};");
            if (data.Data.RewardText.Target != null)
                writer.AppendLine(
                    $"reward_text {data.Data.RewardText.Ids}, {string.Join(", ", data.Data.RewardText.Arguments)};");
            if (data.Data.FailureText.Target != null)
                writer.AppendLine(
                    $"failure_text {data.Data.FailureText.Ids}, {string.Join(", ", data.Data.FailureText.Arguments)};");
            if (data.Data.Difficulty != null)
                writer.AppendLine(
                    $"difficulty {data.Data.Difficulty.Value.X.ToStringInvariant()}, {data.Data.Difficulty.Value.Y.ToStringInvariant()};");
            if (data.Data.Weight != null)
                writer.AppendLine($"weight {data.Data.Weight};");

            foreach (var ot in data.Data.ObjectiveTexts)
            {
                writer.Append($"objective_text {ot.Target}, {ot.Ids}");
                foreach (var a in ot.Arguments)
                    writer.Append($", {a}");
                writer.AppendLine(";");
            }
            if (data.Data.OfferTexts.Count > 0)
            {
                writer.AppendLine("offer_text (");
                writer.Indent();
                for (int oi = 0; oi < data.Data.OfferTexts.Count; oi++)
                {
                    var ot = data.Data.OfferTexts[oi];
                    writer.Append($"{ot.Op}(");
                    for (int i = 0; i < ot.Items.Length; i++)
                    {
                        if (ot.Items[i].Type != OfferTextType.none)
                            writer.Append($"{ot.Items[i].Type}, ");
                        writer.Append($"{ot.Items[i].Ids}");
                        foreach (var arg in ot.Items[i].Args)
                            writer.Append($", {arg}");
                        if (i + 1 < ot.Items.Length)
                            writer.Append(", ");
                    }
                    if(oi + 1 < data.Data.OfferTexts.Count)
                        writer.AppendLine("),");
                    else
                        writer.AppendLine(")");
                }
                writer.UnIndent();
                writer.AppendLine(");");
            }

            foreach (var e in data.Data.CommSequences)
            {
                writer.Append($"comm_sequence {e.Event}, {e.Target}, ");
                writer.Append($"{e.Unknown1.ToStringInvariant()}, {e.Unknown2.ToStringInvariant()}, {e.Unknown3.ToStringInvariant()}");
                writer.AppendLine($", {e.Source}, {e.Comm};");
            }
            foreach (var c in node.Children)
            {
                WriteNode(c, writer, false, references, groups, false);
            }
        }
    }

    public static string Decompile(VignetteParamsIni vparams)
    {
        var tree = VignetteTree.FromIni(vparams);
        var newId = tree.Nodes.Keys.Max() + 1;

        Dictionary<string, string> groups = new Dictionary<string, string>();
        int groupName = 0;

        var tw = new TabbedWriter();
        tw.AppendLine("# Vignette Info Script");
        tw.AppendLine();

        // Collect group names
        void AddGroup(string[] g)
        {
            if (!groups.ContainsKey(FormatGroup(g)))
            {
                var n = $"group_{groupName++}";
                groups[FormatGroup(g)] = n;
                tw.AppendLine($"group {n} {FormatGroup(g)};");
            }
        }

        foreach (var kv in tree.Nodes)
        {
            // Build constants and collect references
            if (kv.Value is AstData dat)
            {
                if (dat.Data.OfferGroup?.Length > 0)
                    AddGroup(dat.Data.OfferGroup);
            }
        }
        tw.AppendLine();

        // Transform branch -> if (group)
        foreach (var kv in tree.Nodes)
        {
            if (kv.Value is AstDecision dec &&
                dec.Decision.Nickname.Equals("branch", StringComparison.OrdinalIgnoreCase))
            {
                if (dec.Children[0] is not AstData d1 ||
                    d1.Data.OfferGroup?.Length <= 0)
                {
                    throw new Exception("Invalid branch node");
                }
                dec.GroupA = d1.Data.OfferGroup;
                if (dec.Children[1] is AstData d2 &&
                    d2.Data.OfferGroup?.Length > 0)
                {
                    dec.GroupB = d2.Data.OfferGroup;
                }
            }
        }

        var queue = new Queue<int>(tree.Nodes.Keys);
        while (queue.Count > 0)
        {
            int nId = queue.Dequeue();
            if (!tree.Nodes.TryGetValue(nId, out var n))
                continue;
            if (n is AstDecision dec)
            {
                if (dec.Children[1] is AstDecision dec2)
                {
                    // Set up if/else node with multiple conditions
                    var ifElse = new AstIfElse(newId++);
                    ifElse.Conditions.Add(dec.GroupA != null ? $"group({groups[FormatGroup(dec.GroupA)]})" : dec.Decision.Nickname);
                    ifElse.Conditions.Add(dec2.GroupA != null ? $"group({groups[FormatGroup(dec2.GroupA)]})" : dec2.Decision.Nickname);
                    if (dec2.GroupB != null)
                        ifElse.Conditions.Add($"group({groups[FormatGroup(dec2.GroupB)]})");
                    ifElse.Children.Add(dec.Children[0]);
                    ifElse.Children.Add(dec2.Children[0]);
                    ifElse.Children.Add(dec2.Children[1]);
                    tree.Replace(dec, ifElse);
                    queue.Enqueue(ifElse.Id);
                }
            }
            else if (n is AstIfElse ie)
            {
                if (ie.Children[^1] is AstDecision dec2)
                {
                    // Add another condition node
                    ie.Children.RemoveAt(ie.Children.Count - 1);
                    ie.Conditions.Add(dec2.GroupA != null ? $"group({groups[FormatGroup(dec2.GroupA)]})" : dec2.Decision.Nickname);
                    if (dec2.GroupB != null)
                    {
                        ie.Conditions.Add($"group({groups[FormatGroup(dec2.GroupB)]})");
                    }
                    ie.Children.Add(dec2.Children[0]);
                    ie.Children.Add(dec2.Children[1]);
                    queue.Enqueue(ie.Id);
                }
            }
        }

        tree.FlattenEmptyNodes(tree.StartNode);
        var references = tree.CullAndGetReferenceCount(tree.StartNode);

        WriteNode(tree.StartNode, tw, false, references, groups, true);

        foreach (var r in references)
        {
            if (r.Value < 2)
                continue;
            if (IsEmptyError(tree.Nodes[r.Key]))
                continue;
            tw.AppendLine();
            tw.AppendLine($"sub node_{r.Key}");
            tw.Indent();
            WriteNode(tree.Nodes[r.Key], tw, false, references, groups, true);
            tw.UnIndent();
            tw.AppendLine("end");
        }

        return tw.ToString();
    }
}
