using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using LibreLancer.Data.Ini;

namespace LancerEdit.GameContent.VignetteNodes;

public enum VignetteNodeKind
{
    Data,
    Decision,
    Documentation,
    Unknown
}

public enum VignetteDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed record VignetteNodeProperty(string Name, string Value, int Line);

public sealed record VignetteGraphEdge(int SourceNodeId, int TargetNodeId, string Relation, bool Broken);

public sealed record VignetteDiagnostic(
    VignetteDiagnosticSeverity Severity,
    int? NodeId,
    string Message);

public sealed class VignetteGraphNode
{
    public required int Index { get; init; }
    public required int Id { get; init; }
    public required string SectionName { get; init; }
    public required VignetteNodeKind Kind { get; init; }
    public required int Line { get; init; }
    public required IReadOnlyList<VignetteNodeProperty> Properties { get; init; }
    public required IReadOnlyList<int> ChildIds { get; init; }
    public required string RawIni { get; init; }
    public List<VignetteGraphEdge> Incoming { get; } = [];
    public List<VignetteGraphEdge> Outgoing { get; } = [];
    public List<VignetteDiagnostic> Diagnostics { get; } = [];
    public bool IsRoot { get; internal set; }
    public bool IsTerminal { get; internal set; }
    public bool IsUnreachable { get; internal set; }
    public bool IsInCycle { get; internal set; }

    public string DisplayName => $"#{Id} {Kind}";
    public string Summary => Kind switch
    {
        VignetteNodeKind.Decision => FirstValue("nickname") ?? "decision",
        VignetteNodeKind.Documentation => FirstValue("documentation") ?? "documentation",
        VignetteNodeKind.Data => DataSummary(),
        _ => SectionName
    };

    public string? FirstValue(string name) =>
        Properties.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;

    public IReadOnlyList<string> Values(string name) =>
        Properties
            .Where(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Value)
            .ToArray();

    public IReadOnlyList<string> SemanticLines() =>
        Kind switch
        {
            VignetteNodeKind.Data => DataSemanticLines().ToArray(),
            VignetteNodeKind.Decision => DecisionSemanticLines().ToArray(),
            VignetteNodeKind.Documentation => DocumentationSemanticLines().ToArray(),
            _ => [SectionName]
        };

    string DataSummary()
    {
        var parts = DataSummaryParts().ToArray();
        return parts.Length == 0 ? "data node" : string.Join(", ", parts);
    }

    IEnumerable<string> DataSummaryParts()
    {
        foreach (var offerText in Values("offer_text").Take(1))
            yield return ShortTupleSummary("offer", offerText);
        foreach (var objectiveText in Values("objective_text").Take(1))
            yield return ShortTupleSummary("objective", objectiveText);
        foreach (var commSequence in Values("comm_sequence").Take(1))
            yield return ShortTupleSummary("comm", commSequence);
        if (FirstValue("weight") is { } weight)
            yield return $"weight {weight}";
        if (FirstValue("difficulty") is { } difficulty)
            yield return $"difficulty {difficulty}";
        if (FirstValue("allowable_zone_types") is { } zone)
            yield return $"zone {zone}";
        if (FirstValue("offer_group") is { } offer)
            yield return $"offer {offer}";
        if (FirstValue("hostile_group") is { } hostile)
            yield return $"hostile {hostile}";
    }

    IEnumerable<string> DataSemanticLines()
    {
        foreach (var value in Values("offer_text"))
            yield return DescribeOfferText(value);
        foreach (var value in Values("objective_text"))
            yield return DescribeObjectiveText(value);
        foreach (var value in Values("Reward_text"))
            yield return DescribeTextTuple("reward_text", value);
        foreach (var value in Values("Failure_text"))
            yield return DescribeTextTuple("failure_text", value);
        foreach (var value in Values("comm_sequence"))
            yield return DescribeCommSequence(value);
        if (FirstValue("difficulty") is { } difficulty)
            yield return $"difficulty: {difficulty}";
        if (FirstValue("weight") is { } weight)
            yield return $"weight: {weight}";
        if (FirstValue("offer_group") is { } offer)
            yield return $"offer groups: {offer}";
        if (FirstValue("hostile_group") is { } hostile)
            yield return $"hostile groups: {hostile}";
        if (FirstValue("allowable_zone_types") is { } zones)
            yield return $"allowable zones: {zones}";
        if (FirstValue("implemented") is { } implemented)
            yield return $"implemented: {implemented}";
    }

    IEnumerable<string> DecisionSemanticLines()
    {
        if (FirstValue("nickname") is { } nickname)
            yield return $"branch: {nickname}";
        yield return ChildIds.Count switch
        {
            0 => "no child nodes",
            1 => $"branch match -> {ChildIds[0]}",
            2 => $"true/left: {ChildIds[0]}, false/right: {ChildIds[1]}",
            _ => $"children: {string.Join(", ", ChildIds)}"
        };
    }

    IEnumerable<string> DocumentationSemanticLines()
    {
        if (FirstValue("documentation") is { } documentation)
            yield return $"documentation: {documentation}";
        if (ChildIds.Count > 0)
            yield return $"next: {string.Join(", ", ChildIds)}";
    }

    static string DescribeOfferText(string value)
    {
        var parts = SplitCsv(value);
        if (parts.Length == 0)
            return "offer_text: empty";
        var mode = IsOfferMode(parts[0]) ? parts[0] : "inline";
        var ids = parts.Where(IsInteger).ToArray();
        var tokens = parts.Where(x => !IsInteger(x) && !IsOfferMode(x)).ToArray();
        return JoinParts("offer_text", mode, ids, tokens);
    }

    static string DescribeObjectiveText(string value)
    {
        var parts = SplitCsv(value);
        if (parts.Length == 0)
            return "objective_text: empty";
        var tokens = parts.Skip(2).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        var ids = parts.Length > 1 && IsInteger(parts[1]) ? new[] { parts[1] } : [];
        var head = parts.Length > 1 ? $"template {parts[0]}" : parts[0];
        return JoinParts("objective_text", head, ids, tokens);
    }

    static string DescribeTextTuple(string field, string value)
    {
        var parts = SplitCsv(value);
        if (parts.Length == 0)
            return $"{field}: empty";
        var ids = IsInteger(parts[0]) ? new[] { parts[0] } : [];
        var tokens = parts.Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        return JoinParts(field, null, ids, tokens);
    }

    static string DescribeCommSequence(string value)
    {
        var parts = SplitCsv(value);
        if (parts.Length < 7)
            return $"comm_sequence: {value}";
        var messages = parts.Skip(6).Where((_, i) => i % 2 == 0).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        var text = $"comm_sequence: event {parts[0]}, receiver {parts[1]}, speaker {parts[5]}";
        if (messages.Length > 0)
            text += $", messages {string.Join(", ", messages)}";
        return text;
    }

    static string JoinParts(string field, string? head, IReadOnlyList<string> ids, IReadOnlyList<string> tokens)
    {
        var result = new List<string>();
        if (!string.IsNullOrWhiteSpace(head))
            result.Add(head);
        if (ids.Count > 0)
            result.Add($"IDS_NAME {string.Join(", ", ids)}");
        if (tokens.Count > 0)
            result.Add($"tokens {string.Join(", ", tokens.Select(DescribeToken))}");
        return result.Count == 0 ? $"{field}: empty" : $"{field}: {string.Join("; ", result)}";
    }

    static string DescribeToken(string token) =>
        token.ToUpperInvariant() switch
        {
            "MISSION_DIFFICULTY" => $"{token} (mission difficulty placeholder)",
            "REWARD_MONEY" => $"{token} (reward money placeholder)",
            "OFFER_BASE" => $"{token} (offer base placeholder)",
            "OFFER_GROUP" => $"{token} (offer faction placeholder)",
            "HOSTILE_GROUP" => $"{token} (hostile faction placeholder)",
            "TARGET_FULL_NAME" => $"{token} (target full name placeholder)",
            "TARGET_ZONE" => $"{token} (target zone placeholder)",
            "OTHER_SOLAR" => $"{token} (secondary solar placeholder)",
            _ => token
        };

    static string ShortTupleSummary(string label, string value)
    {
        var parts = SplitCsv(value);
        if (parts.Length == 0)
            return label;
        if (label.Equals("offer", StringComparison.OrdinalIgnoreCase))
        {
            var mode = IsOfferMode(parts[0]) ? parts[0] : "inline";
            var ids = parts.FirstOrDefault(IsInteger);
            return ids == null ? $"{label} {mode}" : $"{label} {mode} {ids}";
        }
        var first = parts.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        return first == null ? label : $"{label} {first}";
    }

    static string[] SplitCsv(string value) =>
        value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    static bool IsInteger(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

    static bool IsOfferMode(string value) =>
        value.Equals("append", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("replace", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("singular", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("plural", StringComparison.OrdinalIgnoreCase);
}

public sealed class VignetteGraph
{
    public required IReadOnlyList<VignetteGraphNode> Nodes { get; init; }
    public required IReadOnlyList<VignetteGraphEdge> Edges { get; init; }
    public required IReadOnlyList<VignetteDiagnostic> Diagnostics { get; init; }
    public required string SourcePath { get; init; }
    public required string? BackingPath { get; init; }

    public VignetteGraphNode? NodeById(int id) => Nodes.FirstOrDefault(x => x.Id == id);
}

public static class VignetteGraphAnalyzer
{
    private static readonly HashSet<string> KnownEntries = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_id",
        "child_node",
        "nickname",
        "documentation",
        "offer_group",
        "hostile_group",
        "difficulty",
        "weight",
        "allowable_zone_types",
        "implemented",
        "failure_text",
        "reward_text",
        "objective_text",
        "offer_text",
        "comm_sequence"
    };

    public static VignetteGraph FromSections(IEnumerable<Section> sections, string sourcePath, string? backingPath)
    {
        var nodes = sections
            .Where(IsVignetteSection)
            .Select(CreateNode)
            .ToList();
        var diagnostics = new List<VignetteDiagnostic>();
        var byId = new Dictionary<int, VignetteGraphNode>();

        foreach (var node in nodes)
        {
            if (byId.TryAdd(node.Id, node))
                continue;
            AddDiagnostic(diagnostics, node, VignetteDiagnosticSeverity.Error,
                $"Duplicate node_id {node.Id}.");
            AddDiagnostic(diagnostics, byId[node.Id], VignetteDiagnosticSeverity.Error,
                $"Duplicate node_id {node.Id}.");
        }

        var edges = new List<VignetteGraphEdge>();
        foreach (var node in nodes)
        {
            for (int i = 0; i < node.ChildIds.Count; i++)
            {
                var childId = node.ChildIds[i];
                var edge = new VignetteGraphEdge(node.Id, childId, EdgeRelation(node, i), !byId.ContainsKey(childId));
                edges.Add(edge);
                node.Outgoing.Add(edge);
                if (byId.TryGetValue(childId, out var child))
                    child.Incoming.Add(edge);
                else
                    AddDiagnostic(diagnostics, node, VignetteDiagnosticSeverity.Error,
                        $"Broken reference to node_id {childId}.");
            }
        }

        foreach (var node in nodes)
        {
            node.IsRoot = node.Incoming.Count == 0;
            node.IsTerminal = node.Outgoing.Count == 0;
            foreach (var property in node.Properties)
            {
                if (!KnownEntries.Contains(property.Name))
                    AddDiagnostic(diagnostics, node, VignetteDiagnosticSeverity.Info,
                        $"Unknown property '{property.Name}' is preserved.");
            }
        }

        var roots = nodes.Where(x => x.IsRoot).ToArray();
        if (roots.Length == 0 && nodes.Count > 0)
            diagnostics.Add(new(VignetteDiagnosticSeverity.Error, null, "No root node found."));
        if (roots.Length > 1)
            diagnostics.Add(new(VignetteDiagnosticSeverity.Warning, null, $"Multiple root candidates: {string.Join(", ", roots.Select(x => x.Id))}."));

        MarkUnreachable(nodes, roots.FirstOrDefault(), byId, diagnostics);
        MarkCycles(nodes, byId, diagnostics);

        return new VignetteGraph
        {
            Nodes = nodes,
            Edges = edges,
            Diagnostics = diagnostics,
            SourcePath = sourcePath,
            BackingPath = backingPath
        };
    }

    private static bool IsVignetteSection(Section section) =>
        section.Name.Equals("DataNode", StringComparison.OrdinalIgnoreCase) ||
        section.Name.Equals("DecisionNode", StringComparison.OrdinalIgnoreCase) ||
        section.Name.Equals("DocumentationNode", StringComparison.OrdinalIgnoreCase);

    private static VignetteGraphNode CreateNode(Section section, int index)
    {
        var properties = section.Select(e => new VignetteNodeProperty(e.Name, Values(e), e.Line)).ToArray();
        var nodeId = ReadInt(section, "node_id") ?? index;
        var children = section
            .Where(e => e.Name.Equals("child_node", StringComparison.OrdinalIgnoreCase))
            .SelectMany(e => e.Where(v => int.TryParse(v.ToString(), out _)).Select(v => int.Parse(v.ToString())))
            .ToArray();
        var raw = new StringBuilder();
        raw.AppendLine($"[{section.Name}]");
        foreach (var entry in section)
            raw.AppendLine(entry.ToString());
        return new VignetteGraphNode
        {
            Index = index,
            Id = nodeId,
            SectionName = section.Name,
            Kind = Kind(section.Name),
            Line = section.Line,
            Properties = properties,
            ChildIds = children,
            RawIni = raw.ToString().TrimEnd()
        };
    }

    private static VignetteNodeKind Kind(string sectionName) =>
        sectionName.Equals("DataNode", StringComparison.OrdinalIgnoreCase) ? VignetteNodeKind.Data :
        sectionName.Equals("DecisionNode", StringComparison.OrdinalIgnoreCase) ? VignetteNodeKind.Decision :
        sectionName.Equals("DocumentationNode", StringComparison.OrdinalIgnoreCase) ? VignetteNodeKind.Documentation :
        VignetteNodeKind.Unknown;

    private static string EdgeRelation(VignetteGraphNode node, int childIndex) =>
        node.Kind switch
        {
            VignetteNodeKind.Decision when node.ChildIds.Count == 1 => "branch match",
            VignetteNodeKind.Decision => childIndex == 0 ? "true/left" :
                childIndex == 1 ? "false/right" : $"branch option {childIndex + 1}",
            VignetteNodeKind.Documentation => childIndex == 0 ? "next after documentation" : $"next {childIndex + 1}",
            VignetteNodeKind.Data => childIndex == 0 ? "next after data" : $"next {childIndex + 1}",
            _ => childIndex == 0 ? "next" : $"next {childIndex + 1}"
        };

    private static int? ReadInt(Section section, string name)
    {
        var entry = section.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (entry is { Count: > 0 } && int.TryParse(entry[0].ToString(), out var value))
            return value;
        return null;
    }

    private static string Values(Entry entry) =>
        entry.Count == 0 ? "" : string.Join(", ", entry.Select(x => x.ToString()));

    private static void AddDiagnostic(List<VignetteDiagnostic> all, VignetteGraphNode node,
        VignetteDiagnosticSeverity severity, string message)
    {
        var diagnostic = new VignetteDiagnostic(severity, node.Id, message);
        all.Add(diagnostic);
        node.Diagnostics.Add(diagnostic);
    }

    private static void MarkUnreachable(List<VignetteGraphNode> nodes, VignetteGraphNode? root,
        Dictionary<int, VignetteGraphNode> byId, List<VignetteDiagnostic> diagnostics)
    {
        if (root == null)
            return;
        var reachable = new HashSet<int>();
        var stack = new Stack<VignetteGraphNode>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!reachable.Add(node.Id))
                continue;
            foreach (var edge in node.Outgoing)
                if (byId.TryGetValue(edge.TargetNodeId, out var child))
                    stack.Push(child);
        }
        foreach (var node in nodes.Where(x => !reachable.Contains(x.Id)))
        {
            node.IsUnreachable = true;
            AddDiagnostic(diagnostics, node, VignetteDiagnosticSeverity.Warning, "Unreachable from primary root.");
        }
    }

    private static void MarkCycles(List<VignetteGraphNode> nodes, Dictionary<int, VignetteGraphNode> byId,
        List<VignetteDiagnostic> diagnostics)
    {
        var state = new Dictionary<int, int>();
        var stack = new List<VignetteGraphNode>();
        foreach (var node in nodes)
            Visit(node);
        return;

        void Visit(VignetteGraphNode node)
        {
            if (state.GetValueOrDefault(node.Id) == 2)
                return;
            if (state.GetValueOrDefault(node.Id) == 1)
            {
                var idx = stack.FindIndex(x => x.Id == node.Id);
                foreach (var cyc in stack.Skip(Math.Max(0, idx)))
                {
                    if (cyc.IsInCycle)
                        continue;
                    cyc.IsInCycle = true;
                    AddDiagnostic(diagnostics, cyc, VignetteDiagnosticSeverity.Warning, "Cycle detected.");
                }
                return;
            }
            state[node.Id] = 1;
            stack.Add(node);
            foreach (var edge in node.Outgoing)
                if (byId.TryGetValue(edge.TargetNodeId, out var child))
                    Visit(child);
            stack.RemoveAt(stack.Count - 1);
            state[node.Id] = 2;
        }
    }
}
