using System.Linq;
using LancerEdit.GameContent.VignetteNodes;
using LibreLancer.Data.Ini;
using LibreLancer.ContentEdit.RandomMissions;
using LibreLancer.Data.Schema.RandomMissions;
using Xunit;

namespace LibreLancer.Tests;

public class VignetteGraphAnalyzerTests
{
    [Fact]
    public void ValidGraphFindsRootAndTerminal()
    {
        var graph = Analyze(
            Node(1, 2),
            Node(2, 3),
            Node(3));

        Assert.Equal(3, graph.Nodes.Count);
        Assert.Equal(2, graph.Edges.Count);
        Assert.True(graph.NodeById(1)!.IsRoot);
        Assert.True(graph.NodeById(3)!.IsTerminal);
        Assert.DoesNotContain(graph.Diagnostics, x => x.Severity == VignetteDiagnosticSeverity.Error);
    }

    [Fact]
    public void BrokenReferenceIsDiagnosed()
    {
        var graph = Analyze(Node(1, 99));

        Assert.Contains(graph.Edges, x => x.TargetNodeId == 99 && x.Broken);
        Assert.Contains(graph.Diagnostics, x => x.Message.Contains("Broken reference"));
    }

    [Fact]
    public void CycleIsDiagnosed()
    {
        var graph = Analyze(
            Node(1, 2),
            Node(2, 3),
            Node(3, 1));

        Assert.All(graph.Nodes, x => Assert.True(x.IsInCycle));
        Assert.Contains(graph.Diagnostics, x => x.Message.Contains("Cycle"));
    }

    [Fact]
    public void UnreachableNodeIsDiagnosed()
    {
        var graph = Analyze(
            Node(1, 2),
            Node(2),
            Node(9));

        Assert.True(graph.NodeById(9)!.IsUnreachable);
        Assert.Contains(graph.Diagnostics, x => x.NodeId == 9 && x.Message.Contains("Unreachable"));
    }

    [Fact]
    public void UnknownAndRepeatedPropertiesArePreserved()
    {
        var section = Node(1);
        section.Add(Entry(section, "custom_field", "a"));
        section.Add(Entry(section, "custom_field", "b"));

        var graph = Analyze(section);
        var node = graph.NodeById(1)!;

        Assert.Equal(2, node.Properties.Count(x => x.Name == "custom_field"));
        Assert.Contains("custom_field = a", node.RawIni);
        Assert.Contains("custom_field = b", node.RawIni);
        Assert.Contains(graph.Diagnostics, x => x.Message.Contains("Unknown property"));
    }

    [Fact]
    public void DataNodeOfferTextIsDescribedSemantically()
    {
        var section = Node(461, 6);
        section.Add(Entry(section, "offer_text", "replace", 327682, "MISSION_DIFFICULTY", "REWARD_MONEY"));

        var graph = Analyze(section, Node(6));
        var node = graph.NodeById(461)!;
        var semantic = node.SemanticLines();

        Assert.Contains("offer replace 327682", node.Summary);
        Assert.Contains(semantic, x => x.Contains("offer_text: replace") &&
                                       x.Contains("IDS_NAME 327682") &&
                                       x.Contains("MISSION_DIFFICULTY") &&
                                       x.Contains("REWARD_MONEY"));
    }

    [Fact]
    public void EdgeRelationsDescribeNodeKind()
    {
        var data = Node(1, 2);
        var decision = new Section("DecisionNode");
        decision.Add(Entry(decision, "node_id", 2));
        decision.Add(Entry(decision, "nickname", "branch"));
        decision.Add(Entry(decision, "child_node", 3));
        decision.Add(Entry(decision, "child_node", 4));
        var documentation = new Section("DocumentationNode");
        documentation.Add(Entry(documentation, "node_id", 3));
        documentation.Add(Entry(documentation, "documentation", "Main_battle"));
        documentation.Add(Entry(documentation, "child_node", 4));

        var graph = Analyze(data, decision, documentation, Node(4));

        Assert.Contains(graph.Edges, x => x.SourceNodeId == 1 && x.Relation == "next after data");
        Assert.Contains(graph.Edges, x => x.SourceNodeId == 2 && x.TargetNodeId == 3 && x.Relation == "true/left");
        Assert.Contains(graph.Edges, x => x.SourceNodeId == 2 && x.TargetNodeId == 4 && x.Relation == "false/right");
        Assert.Contains(graph.Edges, x => x.SourceNodeId == 3 && x.Relation == "next after documentation");
    }

    [Fact]
    public void SingleChildDecisionIsABranchMatchNotTrueFalse()
    {
        var decision = new Section("DecisionNode");
        decision.Add(Entry(decision, "node_id", 5));
        decision.Add(Entry(decision, "nickname", "Assassinate_mission"));
        decision.Add(Entry(decision, "child_node", 173));

        var graph = Analyze(decision, Node(173));

        Assert.Contains(graph.Edges, x => x.SourceNodeId == 5 && x.TargetNodeId == 173 && x.Relation == "branch match");
        Assert.Contains(graph.NodeById(5)!.SemanticLines(), x => x == "branch match -> 173");
    }

    [Fact]
    public void SingleChildDecisionDecompilesToCompilableDsl()
    {
        var vparams = new VignetteParamsIni();
        vparams.Nodes.Add(new DecisionNode
        {
            NodeId = 5,
            Nickname = "Assassinate_mission",
            ChildId = { 173 }
        });
        vparams.Nodes.Add(new DataNode
        {
            NodeId = 173,
            Implemented = false
        });

        var script = VignetteParamsDecompiler.Decompile(vparams);
        var sections = VignetteParamsCompiler.Compile(script, "single-child-decision");

        Assert.Contains("noop;", script);
        Assert.DoesNotContain("err_unimplemented", script);
        Assert.NotEmpty(sections);
    }

    [Fact]
    public void NoopAliasCompilesAsEmptyBranchPlaceholder()
    {
        var sections = VignetteParamsCompiler.Compile("""
            if Some_condition
                noop;
            else
                noop;
            end
            """, "noop-test");

        Assert.NotEmpty(sections);
    }

    private static VignetteGraph Analyze(params Section[] sections) =>
        VignetteGraphAnalyzer.FromSections(sections, "vignetteparams.ini", null);

    private static Section Node(int id, params int[] children)
    {
        var section = new Section("DataNode");
        section.Add(Entry(section, "node_id", id));
        foreach (var child in children)
            section.Add(Entry(section, "child_node", child));
        return section;
    }

    private static Entry Entry(Section section, string name, params ValueBase[] values)
    {
        var entry = new Entry(section, name);
        foreach (var value in values)
            entry.Add(value);
        return entry;
    }
}
