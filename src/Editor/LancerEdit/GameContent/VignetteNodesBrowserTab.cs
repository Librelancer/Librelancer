using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using ImGuiNET;
using LancerEdit.GameContent.VignetteNodes;
using LibreLancer;
using LibreLancer.ContentEdit.RandomMissions;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.RandomMissions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;

namespace LancerEdit.GameContent;

public sealed class VignetteNodesBrowserTab : GameContentTab
{
    private readonly MainWindow win;
    private readonly GameDataContext context;
    private readonly string sourcePath;
    private VignetteGraph graph;
    private VignetteGraphNode? selected;
    private VignetteGraphNode[] visibleNodes = [];
    private readonly NodeEditorConfig graphConfig;
    private readonly NodeEditorContext graphContext;
    private readonly ColorTextEdit treeEditor;
    private readonly Queue<(NodeId Id, Vector2 Pos)> graphPositions = [];
    private bool graphNeedsLayout;
    private bool graphNavigateToSelected;
    private bool showIdsText = true;
    private bool showFactionNames = true;
    private bool showFlowHints = true;
    private string search = "";
    private string treeText = "";
    private string[] treeLines = [];
    private string treeCompileStatus = "";
    private string treeSearch = "";
    private string treeSearchStatus = "";
    private int pendingTreeJumpLine = -1;
    private int highlightedTreeLine = -1;
    private string treeSenseSearch = "";
    private int treeSearchIndex;
    private int treeSenseFilter;
    private int kindFilter;
    private int stateFilter;

    private static readonly string[] KindFilters =
    [
        "All",
        "Data",
        "Decision",
        "Documentation",
        "Unknown"
    ];

    private static readonly string[] StateFilters =
    [
        "All",
        "Has outgoing links",
        "Has incoming links",
        "No incoming links",
        "No outgoing links",
        "Broken references",
        "Unknown type",
        "Unreachable"
    ];

    private static readonly string[] TreeSenseFilters =
    [
        "All",
        "Syntax",
        "Conditions",
        "Groups",
        "Factions",
        "IDS_NAME",
        "Nodes"
    ];

    public VignetteNodesBrowserTab(MainWindow win, GameDataContext context)
    {
        this.win = win;
        this.context = context;
        Title = "Vignette Nodes Browser";
        graphConfig = new NodeEditorConfig { SettingsFile = null };
        graphContext = new NodeEditorContext(graphConfig);
        treeEditor = new ColorTextEdit();
        treeEditor.SetMode(ColorTextEditMode.Lua);
        treeEditor.SetReadOnly(true);
        NodeBuilder.LoadTexture(win.RenderContext);
        sourcePath = context.GameData.Items.Ini.Freelancer.DataPath + "randommissions\\vignetteparams.ini";
        Reload();
    }

    public override void Draw(double elapsed)
    {
        DrawToolbar();
        ImGui.Separator();
        if (graph == null)
            return;

        var avail = ImGui.GetContentRegionAvail();
        var leftWidth = Math.Clamp(avail.X * 0.30f, 260 * ImGuiHelper.Scale, 420 * ImGuiHelper.Scale);
        ImGui.BeginChild("##vignetteNodeList", new Vector2(leftWidth, 0), ImGuiChildFlags.Borders);
        DrawNodeList();
        ImGui.EndChild();

        ImGui.SameLine();
        ImGui.BeginChild("##vignetteNodeDetails", new Vector2(0, 0), ImGuiChildFlags.Borders);
        DrawDetails();
        ImGui.EndChild();
    }

    private void DrawToolbar()
    {
        ImGui.Text("VignetteParams.ini");
        ImGui.SameLine();
        ImGui.TextDisabled(graph?.BackingPath ?? sourcePath);
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.SyncAlt}##refreshVignetteNodes"))
            Reload();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Reload from game data.");

        ImGui.SetNextItemWidth(300 * ImGuiHelper.Scale);
        if (ImGui.InputTextWithHint("##vignetteSearch", "Search nodes, properties, links, raw INI", ref search, 256))
            RebuildVisibleNodes();
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.X}##clearVignetteSearch", new Vector2(32 * ImGuiHelper.Scale, 0)))
        {
            search = "";
            RebuildVisibleNodes();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Clear search.");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150 * ImGuiHelper.Scale);
        if (ImGui.Combo("Type", ref kindFilter, KindFilters, KindFilters.Length))
            RebuildVisibleNodes();
        ImGui.SameLine();
        ImGui.SetNextItemWidth(180 * ImGuiHelper.Scale);
        if (ImGui.Combo("State", ref stateFilter, StateFilters, StateFilters.Length))
            RebuildVisibleNodes();
        ImGui.SameLine();
        ImGui.TextDisabled($"{visibleNodes.Length}/{graph?.Nodes.Count ?? 0} nodes");
        ImGui.SameLine();
        ImGui.Checkbox("IDS text", ref showIdsText);
        ImGui.SameLine();
        ImGui.Checkbox("Faction names", ref showFactionNames);
        ImGui.SameLine();
        ImGui.Checkbox("Flow hints", ref showFlowHints);
    }

    private void Reload()
    {
        if (!context.GameData.VFS.FileExists(sourcePath))
        {
            win.Popups.MessageBox("Vignette Nodes Browser", $"{sourcePath} was not found in loaded game data.");
            graph = new VignetteGraph
            {
                Nodes = [],
                Edges = [],
                Diagnostics = [new(VignetteDiagnosticSeverity.Error, null, "VignetteParams.ini was not found.")],
                SourcePath = sourcePath,
                BackingPath = null
            };
            selected = null;
            treeText = "";
            treeLines = [];
            treeEditor.SetText(treeText);
            graphNeedsLayout = true;
            RebuildVisibleNodes();
            return;
        }

        var sections = IniFile.ParseFile(sourcePath, context.GameData.VFS).ToArray();
        graph = VignetteGraphAnalyzer.FromSections(sections, sourcePath, context.GameData.VFS.GetBackingFileName(sourcePath));
        treeText = BuildTreeText();
        treeLines = SplitTreeLines(treeText);
        treeEditor.SetText(treeText);
        selected = graph.Nodes.FirstOrDefault(x => x.IsRoot) ?? graph.Nodes.FirstOrDefault();
        graphNeedsLayout = true;
        graphNavigateToSelected = true;
        RebuildVisibleNodes();
    }

    private void RebuildVisibleNodes()
    {
        IEnumerable<VignetteGraphNode> nodes = graph?.Nodes ?? [];
        if (kindFilter > 0)
        {
            var kind = (VignetteNodeKind)(kindFilter - 1);
            nodes = nodes.Where(x => x.Kind == kind);
        }
        nodes = stateFilter switch
        {
            1 => nodes.Where(x => x.Outgoing.Count > 0),
            2 => nodes.Where(x => x.Incoming.Count > 0),
            3 => nodes.Where(x => x.Incoming.Count == 0),
            4 => nodes.Where(x => x.Outgoing.Count == 0),
            5 => nodes.Where(x => x.Outgoing.Any(e => e.Broken)),
            6 => nodes.Where(x => x.Kind == VignetteNodeKind.Unknown),
            7 => nodes.Where(x => x.IsUnreachable),
            _ => nodes
        };
        if (!string.IsNullOrWhiteSpace(search))
            nodes = nodes.Where(MatchesSearch);
        visibleNodes = nodes.OrderBy(x => x.Id).ToArray();
        if (selected != null && !visibleNodes.Contains(selected))
            selected = visibleNodes.FirstOrDefault();
    }

    private bool MatchesSearch(VignetteGraphNode node)
    {
        if (node.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
            node.Kind.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
            node.SectionName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            node.RawIni.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            node.Summary.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            HumanSemanticLines(node).Any(x => x.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
            ResolvedTextLines(node).Any(x => x.Contains(search, StringComparison.OrdinalIgnoreCase)))
            return true;
        return node.Properties.Any(x =>
                   x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                   x.Value.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
               node.Incoming.Concat(node.Outgoing).Any(x =>
                   x.SourceNodeId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                   x.TargetNodeId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                   x.Relation.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    private void DrawNodeList()
    {
        if (visibleNodes.Length == 0)
        {
            ImGui.TextDisabled("No nodes match the current filters.");
            return;
        }

        var clipper = new ImGuiListClipper();
        clipper.Begin(visibleNodes.Length);
        while (clipper.Step())
        {
            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                var node = visibleNodes[i];
                var flags = StatusFlags(node);
                if (ImGui.Selectable($"{node.DisplayName}  {flags}##vnode_{node.Id}_{i}", selected == node))
                {
                    selected = node;
                    graphNavigateToSelected = true;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip($"{node.Summary}\nLine {node.Line}");
                ImGui.TextDisabled(node.Summary);
            }
        }
        clipper.End();
    }

    private static string StatusFlags(VignetteGraphNode node)
    {
        var flags = new List<string>();
        if (node.IsRoot)
            flags.Add("root");
        if (node.IsTerminal)
            flags.Add("terminal");
        if (node.Outgoing.Any(x => x.Broken))
            flags.Add("broken");
        if (node.IsInCycle)
            flags.Add("cycle");
        if (node.IsUnreachable)
            flags.Add("unreachable");
        return flags.Count == 0 ? "" : $"({string.Join(", ", flags)})";
    }

    private void DrawDetails()
    {
        if (selected == null)
        {
            ImGui.TextDisabled("Select a node.");
            return;
        }

        ImGui.Text($"{selected.DisplayName}");
        ImGui.SameLine();
        ImGui.TextDisabled($"line {selected.Line}");
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.Copy}##copyNodeId", new Vector2(34 * ImGuiHelper.Scale, 0)))
            win.SetClipboardText(selected.Id.ToString());
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Copy node identifier.");
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.FileExport}##exportDiagnostics", new Vector2(34 * ImGuiHelper.Scale, 0)))
            win.TextWindows.Add(new TextDisplayWindow(BuildDiagnosticExport(), "vignette-diagnostics.txt", win));
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Open diagnostic export.");

        if (!ImGui.BeginTabBar("##vignetteNodeTabs"))
            return;
        if (ImGui.BeginTabItem("Semantic"))
        {
            DrawSemantic();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Properties"))
        {
            DrawProperties();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Connections"))
        {
            DrawConnections();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Raw INI"))
        {
            DrawRawIni();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Diagnostics"))
        {
            DrawDiagnostics();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Tree"))
        {
            DrawTree();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Graph"))
        {
            DrawGraph();
            ImGui.EndTabItem();
        }
        ImGui.EndTabBar();
    }

    private void DrawSemantic()
    {
        if (showFlowHints)
        {
            ImGui.Text("Flow");
            foreach (var line in FlowLines(selected!))
                ImGui.BulletText(line);
            ImGui.Separator();
        }

        ImGui.Text("Fields");
        var lines = HumanSemanticLines(selected!).ToArray();
        if (lines.Length == 0)
        {
            ImGui.TextDisabled("No semantic fields on this node.");
        }
        else
        {
            foreach (var line in lines)
                ImGui.BulletText(line);
        }

        if (!showIdsText)
            return;

        var resolved = ResolvedTextLines(selected).ToArray();
        if (resolved.Length == 0)
            return;

        ImGui.Separator();
        ImGui.Text("Resolved IDS_NAME text");
        foreach (var line in resolved)
            ImGui.TextWrapped(line);
    }

    private void DrawTree()
    {
        if (ImGui.Button($"{Icons.Copy} Copy tree"))
            win.SetClipboardText(treeText);
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.FileExport} Compile preview"))
            CompileTreePreview();
        ImGui.SameLine();
        ImGui.TextDisabled("Read-only DSL. # comments are ignored by the compiler.");
        if (!string.IsNullOrWhiteSpace(treeCompileStatus))
        {
            ImGui.SameLine();
            ImGui.TextDisabled(treeCompileStatus);
        }

        DrawTreeSearch();
        DrawTreeIntelliSense();
        DrawTreeHelp();
        treeEditor.Render("##vignetteTreeEditor");
    }

    private void DrawTreeViewer()
    {
        var lineHeight = ImGui.GetTextLineHeightWithSpacing();
        if (ImGui.BeginChild("##vignetteTreeViewer", Vector2.Zero, ImGuiChildFlags.Borders,
                ImGuiWindowFlags.HorizontalScrollbar))
        {
            if (pendingTreeJumpLine > 0)
            {
                var targetY = Math.Max(0, (pendingTreeJumpLine - 1) * lineHeight - ImGui.GetWindowHeight() * 0.35f);
                ImGui.SetScrollY(targetY);
                pendingTreeJumpLine = -1;
            }

            ImGui.PushFont(ImGuiHelper.SystemMonospace, 0);
            var clipper = new ImGuiListClipper();
            clipper.Begin(treeLines.Length);
            while (clipper.Step())
            {
                for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    DrawTreeViewerLine(i + 1, treeLines[i]);
            }
            clipper.End();
            ImGui.PopFont();
        }
        ImGui.EndChild();
    }

    private void DrawTreeViewerLine(int lineNumber, string line)
    {
        var lineNumberWidth = 58 * ImGuiHelper.Scale;
        ImGui.TextColored(new Vector4(0.48f, 0.50f, 0.58f, 1), lineNumber.ToString(CultureInfo.InvariantCulture).PadLeft(5));
        ImGui.SameLine(lineNumberWidth);
        var color = lineNumber == highlightedTreeLine
            ? new Vector4(1.0f, 0.88f, 0.45f, 1)
            : TreeLineColor(line);
        ImGui.TextColored(color, line.Length == 0 ? " " : line);
    }

    private static Vector4 TreeLineColor(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("#", StringComparison.Ordinal))
            return new Vector4(0.20f, 0.85f, 0.52f, 1);
        if (trimmed.StartsWith("if ", StringComparison.Ordinal) ||
            trimmed.StartsWith("elif ", StringComparison.Ordinal) ||
            trimmed == "else" ||
            trimmed == "end")
            return new Vector4(0.90f, 0.55f, 0.90f, 1);
        if (trimmed.StartsWith("group ", StringComparison.Ordinal) ||
            trimmed.StartsWith("doc ", StringComparison.Ordinal) ||
            trimmed.StartsWith("sub ", StringComparison.Ordinal) ||
            trimmed.StartsWith("call ", StringComparison.Ordinal))
            return new Vector4(0.45f, 0.82f, 1.0f, 1);
        return new Vector4(0.88f, 0.90f, 0.94f, 1);
    }

    private void DrawTreeSearch()
    {
        ImGui.SetNextItemWidth(280 * ImGuiHelper.Scale);
        if (ImGui.InputTextWithHint("##vignetteTreeSearch", "Search DSL, factions, IDS_NAME, node_id", ref treeSearch, 256))
        {
            treeSearchIndex = 0;
            treeSearchStatus = "";
        }
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.X}##clearVignetteTreeSearch", new Vector2(32 * ImGuiHelper.Scale, 0)))
        {
            treeSearch = "";
            treeSearchIndex = 0;
            treeSearchStatus = "";
        }

        var matches = TreeSearchMatches(treeSearch).ToArray();
        ImGui.SameLine();
        ImGui.TextDisabled(string.IsNullOrWhiteSpace(treeSearch) ? "Search in generated DSL and # hints." : $"{matches.Length} match(es)");

        if (matches.Length == 0)
            return;

        treeSearchIndex = Math.Clamp(treeSearchIndex, 0, matches.Length - 1);
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.ArrowUp}##prevVignetteTreeSearch", new Vector2(32 * ImGuiHelper.Scale, 0)))
        {
            treeSearchIndex = (treeSearchIndex + matches.Length - 1) % matches.Length;
            NavigateTreeToLine(matches[treeSearchIndex].Line);
        }
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.ArrowDown}##nextVignetteTreeSearch", new Vector2(32 * ImGuiHelper.Scale, 0)))
        {
            treeSearchIndex = (treeSearchIndex + 1) % matches.Length;
            NavigateTreeToLine(matches[treeSearchIndex].Line);
        }

        var current = matches[treeSearchIndex];
        ImGui.SameLine();
        ImGui.TextDisabled($"Line {current.Line}: {current.Text}");
        if (!string.IsNullOrEmpty(treeSearchStatus))
        {
            ImGui.SameLine();
            ImGui.TextDisabled(treeSearchStatus);
        }

        if (matches.Length <= 1)
            return;

        var previewCount = Math.Min(6, matches.Length);
        if (ImGui.BeginChild("##vignetteTreeSearchResults", new Vector2(0, (previewCount * 22 + 8) * ImGuiHelper.Scale),
                ImGuiChildFlags.Borders))
        {
            for (int i = 0; i < previewCount; i++)
            {
                var matchIndex = (treeSearchIndex + i) % matches.Length;
                var match = matches[matchIndex];
                if (ImGui.Selectable($"{match.Line,5}: {match.Text}", matchIndex == treeSearchIndex))
                {
                    treeSearchIndex = matchIndex;
                    NavigateTreeToLine(match.Line);
                }
            }
        }
        ImGui.EndChild();
    }

    private void NavigateTreeToLine(int line)
    {
        highlightedTreeLine = line;
        treeSearchStatus = treeEditor.GoToLine(line)
            ? ""
            : "Line jump needs rebuilt cimgui native DLL.";
    }

    private void DrawTreeHelp()
    {
        if (!ImGui.CollapsingHeader($"{Icons.QuestionCircle} DSL help"))
            return;

        ImGui.TextWrapped("This is a readable view of vignetteparams.ini. # lines are hints ignored by the compiler. Statements end with ;, so ; is not used as a comment marker.");
        ImGui.Separator();
        if (ImGui.BeginTable("##vignetteTreeHelpTable", 2,
                ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("Syntax", ImGuiTableColumnFlags.WidthStretch, 0.45f);
            ImGui.TableSetupColumn("Meaning", ImGuiTableColumnFlags.WidthStretch, 1.0f);
            ImGui.TableHeadersRow();
            DrawTreeHelpLine("group name factions...;", "Named faction list reused by branch conditions and offer/hostile groups.");
            DrawTreeHelpLine("doc Name;", "Creates a DocumentationNode / logical section.");
            DrawTreeHelpLine("if Condition / elif group(name) / else / end", "Creates DecisionNode branching. group(name) expands to a faction-group condition.");
            DrawTreeHelpLine("sub node_123 ... end", "Reusable branch body. The decompiler emits a sub when the same node is referenced more than once.");
            DrawTreeHelpLine("call node_123;", "Jumps into a reusable sub branch.");
            DrawTreeHelpLine("offer_group / hostile_group", "Defines offer factions and enemy factions for the current mission branch.");
            DrawTreeHelpLine("offer_text / objective_text", "Adds IDS_NAME-backed mission text with runtime tokens.");
            DrawTreeHelpLine("comm_sequence", "Adds communication event lines for mission radio/dialog messages.");
            DrawTreeHelpLine("noop;", "Empty branch placeholder for an alternative that was missing in the original INI.");
            ImGui.EndTable();
        }
    }

    private static void DrawTreeHelpLine(string syntax, string description)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.PushFont(ImGuiHelper.SystemMonospace, 0);
        ImGui.Text(syntax);
        ImGui.PopFont();
        ImGui.TableNextColumn();
        ImGui.TextWrapped(description);
    }

    private IEnumerable<(int Line, string Text)> TreeSearchMatches(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            yield break;

        for (int i = 0; i < treeLines.Length; i++)
        {
            if (treeLines[i].IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            yield return (i + 1, TrimSearchResult(treeLines[i]));
        }
    }

    private static string TrimSearchResult(string line)
    {
        line = line.Trim();
        const int max = 160;
        return line.Length <= max ? line : line[..max] + "...";
    }

    private static string[] SplitTreeLines(string text)
    {
        return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    }

    private void DrawTreeIntelliSense()
    {
        if (!ImGui.CollapsingHeader($"{Icons.Lightbulb} IntelliSense"))
            return;

        ImGui.SetNextItemWidth(220 * ImGuiHelper.Scale);
        ImGui.Combo("##vignetteTreeSenseKind", ref treeSenseFilter, TreeSenseFilters, TreeSenseFilters.Length);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(320 * ImGuiHelper.Scale);
        ImGui.InputTextWithHint("##vignetteTreeSenseSearch", "Filter keys, factions, IDS_NAME, nodes", ref treeSenseSearch, 256);
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.X}##clearVignetteTreeSense", new Vector2(32 * ImGuiHelper.Scale, 0)))
            treeSenseSearch = "";

        var items = BuildTreeSenseItems()
            .Where(MatchesTreeSenseFilter)
            .Take(300)
            .ToArray();

        ImGui.TextDisabled($"{items.Length} suggestion(s). Click {Icons.Copy} to copy insertion text.");
        if (!ImGui.BeginTable("##vignetteTreeSenseTable", 4,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable,
                new Vector2(0, 220 * ImGuiHelper.Scale)))
            return;

        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 34 * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("Kind", ImGuiTableColumnFlags.WidthFixed, 90 * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("Insert", ImGuiTableColumnFlags.WidthStretch, 0.8f);
        ImGui.TableSetupColumn("Hint", ImGuiTableColumnFlags.WidthStretch, 1.8f);
        ImGui.TableHeadersRow();

        foreach (var item in items)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            if (ImGui.Button($"{Icons.Copy}##copySense{item.Kind}{item.InsertText}"))
                win.SetClipboardText(item.InsertText);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Copy insertion text");

            ImGui.TableNextColumn();
            ImGui.Text(item.Kind);
            ImGui.TableNextColumn();
            ImGui.PushFont(ImGuiHelper.SystemMonospace, 0);
            ImGui.TextWrapped(item.InsertText);
            ImGui.PopFont();
            ImGui.TableNextColumn();
            ImGui.TextWrapped(item.Hint);
            if (!string.IsNullOrWhiteSpace(item.Details) && ImGui.IsItemHovered())
                ImGui.SetTooltip(item.Details);
        }
        ImGui.EndTable();
    }

    private bool MatchesTreeSenseFilter(TreeSenseItem item)
    {
        if (treeSenseFilter > 0 &&
            !item.Kind.Equals(TreeSenseFilters[treeSenseFilter], StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.IsNullOrWhiteSpace(treeSenseSearch))
            return true;
        return item.InsertText.Contains(treeSenseSearch, StringComparison.OrdinalIgnoreCase) ||
               item.Hint.Contains(treeSenseSearch, StringComparison.OrdinalIgnoreCase) ||
               item.Details.Contains(treeSenseSearch, StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<TreeSenseItem> BuildTreeSenseItems()
    {
        foreach (var item in BuildSyntaxSenseItems())
            yield return item;

        foreach (var condition in graph.Nodes
                     .Where(x => x.Kind == VignetteNodeKind.Decision)
                     .Select(x => x.FirstValue("nickname"))
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(x => x))
        {
            yield return new TreeSenseItem("Conditions", condition!, $"Decision condition: {condition}", "Use after if/elif for mission branch checks.");
        }

        var groupDefinitions = ExtractGroupDefinitions(treeLines);
        foreach (var group in groupDefinitions.OrderBy(x => x.Key))
        {
            yield return new TreeSenseItem("Groups", group.Key, $"group({group.Key}) -> {HumanizeFactionGroup(group.Value)}",
                $"Raw factions: {group.Value}");
        }

        foreach (var faction in context.GameData.Items.Factions.OrderBy(x => x.Nickname))
        {
            var name = ResolveStringId(faction.IdsName);
            var hint = string.IsNullOrWhiteSpace(name)
                ? faction.Nickname
                : $"{faction.Nickname} -> {NormalizeResolvedText(name)}";
            yield return new TreeSenseItem("Factions", faction.Nickname, hint, $"IDS_NAME {faction.IdsName}");
        }

        foreach (var id in TreeIds().OrderBy(x => x))
        {
            var text = ResolveStringId(id);
            var hint = string.IsNullOrWhiteSpace(text)
                ? $"IDS_NAME {id}"
                : $"IDS_NAME {id}: {Shorten(NormalizeResolvedText(text), 180)}";
            yield return new TreeSenseItem("IDS_NAME", id.ToString(CultureInfo.InvariantCulture), hint, hint);
        }

        foreach (var node in graph.Nodes.OrderBy(x => x.Id))
        {
            yield return new TreeSenseItem("Nodes", $"call node_{node.Id};", $"#{node.Id} {node.DisplayName}: {node.Summary}",
                string.Join("\n", HumanSemanticLines(node).Take(5)));
        }
    }

    private static IEnumerable<TreeSenseItem> BuildSyntaxSenseItems()
    {
        yield return new TreeSenseItem("Syntax", "group group_name fc_x_grp, fc_y_grp;", "Define a reusable faction group.", "RU: список фракций. EN: named faction list.");
        yield return new TreeSenseItem("Syntax", "doc DocumentationName;", "Create a documentation/logical section.", "RU: DocumentationNode. EN: documentation node.");
        yield return new TreeSenseItem("Syntax", "if Condition", "Start a DecisionNode branch.", "RU: условная ветка. EN: conditional branch.");
        yield return new TreeSenseItem("Syntax", "elif group(group_name)", "Faction-group branch condition.", "RU: ветка по группе фракций. EN: faction group branch.");
        yield return new TreeSenseItem("Syntax", "else", "Fallback branch.", "RU: ветка иначе. EN: fallback branch.");
        yield return new TreeSenseItem("Syntax", "end", "Close if/sub block.", "RU: закрывает блок. EN: closes a block.");
        yield return new TreeSenseItem("Syntax", "sub node_123", "Reusable branch body.", "RU: переиспользуемая ветка. EN: reusable branch.");
        yield return new TreeSenseItem("Syntax", "call node_123;", "Call a reusable branch.", "RU: переход к sub. EN: call a sub branch.");
        yield return new TreeSenseItem("Syntax", "offer_group group_name;", "Set offer factions.", "RU: фракции оффера. EN: offer factions.");
        yield return new TreeSenseItem("Syntax", "hostile_group group_name;", "Set hostile factions.", "RU: враждебные фракции. EN: hostile factions.");
        yield return new TreeSenseItem("Syntax", "offer_text (...);", "Mission offer text sequence.", "RU: текст оффера. EN: offer text.");
        yield return new TreeSenseItem("Syntax", "objective_text template_name, IDS_NAME;", "Objective text template.", "RU: текст цели. EN: objective text.");
        yield return new TreeSenseItem("Syntax", "comm_sequence EVENT, TARGET, 0, 0, 0, SOURCE, MESSAGE;", "Communication event.", "RU: реплика/событие. EN: comm event.");
        yield return new TreeSenseItem("Syntax", "noop;", "Empty branch placeholder.", "RU: пустая ветка, которой не было в исходном INI. EN: empty branch missing from the original INI.");
    }

    private IEnumerable<int> TreeIds()
    {
        var seen = new HashSet<int>();
        foreach (var line in treeLines)
        {
            foreach (var id in SplitIdsFromCodeLine(line))
            {
                if (seen.Add(id))
                    yield return id;
            }
        }
    }

    private readonly record struct TreeSenseItem(string Kind, string InsertText, string Hint, string Details);

    private void DrawProperties()
    {
        if (!ImGui.BeginTable("##vignetteProperties", 3,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
            return;
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 0.8f);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch, 2.2f);
        ImGui.TableSetupColumn("Line", ImGuiTableColumnFlags.WidthFixed, 60 * ImGuiHelper.Scale);
        ImGui.TableHeadersRow();
        foreach (var property in selected!.Properties)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(property.Name);
            ImGui.TableNextColumn();
            ImGui.TextWrapped(property.Value);
            ImGui.TableNextColumn();
            ImGui.Text(property.Line.ToString());
        }
        ImGui.EndTable();
    }

    private void DrawConnections()
    {
        ImGui.Text("Incoming");
        DrawEdgeList(selected!.Incoming, true);
        ImGui.Separator();
        ImGui.Text("Outgoing");
        DrawEdgeList(selected.Outgoing, false);
    }

    private void DrawEdgeList(IEnumerable<VignetteGraphEdge> edges, bool incoming)
    {
        var any = false;
        foreach (var edge in edges)
        {
            any = true;
            var otherId = incoming ? edge.SourceNodeId : edge.TargetNodeId;
            var label = incoming
                ? $"{edge.SourceNodeId} -> {edge.TargetNodeId} ({edge.Relation})"
                : $"{edge.SourceNodeId} -> {edge.TargetNodeId} ({edge.Relation})";
            if (edge.Broken)
                ImGui.TextColored(new Vector4(0.95f, 0.35f, 0.28f, 1), label + " broken");
            else if (ImGui.Selectable(label))
            {
                selected = graph.NodeById(otherId);
                graphNavigateToSelected = true;
            }
        }
        if (!any)
            ImGui.TextDisabled("None");
    }

    private void DrawRawIni()
    {
        if (ImGui.Button($"{Icons.Copy} Copy raw section"))
            win.SetClipboardText(selected!.RawIni);
        ImGui.PushFont(ImGuiHelper.SystemMonospace, 0);
        var raw = selected!.RawIni;
        ImGui.InputTextMultiline("##rawVignetteNode", ref raw, (uint)Encoding.UTF8.GetByteCount(raw) + 1,
            ImGui.GetContentRegionAvail(), ImGuiInputTextFlags.ReadOnly);
        ImGui.PopFont();
    }

    private void DrawDiagnostics()
    {
        if (selected!.Diagnostics.Count == 0 && graph.Diagnostics.Count == 0)
        {
            ImGui.TextDisabled("No diagnostics.");
            return;
        }
        foreach (var diagnostic in graph.Diagnostics.Where(x => x.NodeId == null || x.NodeId == selected.Id))
        {
            var color = diagnostic.Severity switch
            {
                VignetteDiagnosticSeverity.Error => new Vector4(0.95f, 0.35f, 0.28f, 1),
                VignetteDiagnosticSeverity.Warning => new Vector4(1f, 0.72f, 0.25f, 1),
                _ => new Vector4(0.65f, 0.75f, 0.95f, 1)
            };
            ImGui.TextColored(color, $"{diagnostic.Severity}: {diagnostic.Message}");
        }
    }

    private void DrawGraph()
    {
        if (ImGui.Button($"{Icons.Sitemap} Fit graph"))
            graphNeedsLayout = true;
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.Bullseye} Center selected"))
            graphNavigateToSelected = true;
        ImGui.SameLine();
        ImGui.TextDisabled("Read-only graph canvas. Pan and zoom like Mission Script Editor.");

        NodeEditor.SetCurrentEditor(graphContext);
        NodeEditor.Begin("Vignette Node Graph", ImGui.GetContentRegionAvail());

        if (graphNeedsLayout)
        {
            QueueGraphLayout();
            graphNeedsLayout = false;
        }

        while (graphPositions.Count > 0)
        {
            var item = graphPositions.Dequeue();
            NodeEditor.SetNodePosition(item.Id, item.Pos);
        }

        foreach (var node in graph.Nodes)
            DrawGraphCanvasNode(node);

        for (int i = 0; i < graph.Edges.Count; i++)
        {
            var edge = graph.Edges[i];
            if (edge.Broken)
                continue;
            var source = graph.NodeById(edge.SourceNodeId);
            var target = graph.NodeById(edge.TargetNodeId);
            if (source == null || target == null)
                continue;
            NodeEditor.Link(GraphLinkId(i), GraphOutputPin(source), GraphInputPin(target), EdgeColor(edge), 2.0f);
        }

        SyncGraphSelection();

        if (graphNavigateToSelected && selected != null)
        {
            NodeEditor.ClearSelection();
            NodeEditor.SelectNode(GraphNodeId(selected));
            NodeEditor.NavigateToSelection(true);
            graphNavigateToSelected = false;
        }

        NodeEditor.End();
        NodeEditor.SetCurrentEditor(null);
    }

    private void DrawGraphCanvasNode(VignetteGraphNode node)
    {
        var dim = search is not "" && !MatchesSearch(node);
        if (dim)
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.25f);

        NodeEditor.PushStyleVar(StyleVar.NodePadding, new Vector4(8, 4, 8, 8));
        NodeEditor.BeginNode(GraphNodeId(node));
        ImGui.PushID(node.Index);

        var headerColor = NodeColor(node);
        ImGui.PushStyleColor(ImGuiCol.Text, headerColor);
        ImGui.BeginGroup();
        ImGui.Text($"{node.DisplayName}");
        ImGui.SameLine();
        ImGui.TextDisabled(StatusFlags(node));
        ImGui.EndGroup();
        ImGui.PopStyleColor();

        ImGui.BeginTable("##nodePins", 3, ImGuiTableFlags.PreciseWidths, new Vector2(420, 0));
        ImGui.TableSetupColumn("Input", ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("Summary", ImGuiTableColumnFlags.WidthFixed, 280);
        ImGui.TableSetupColumn("Output", ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        NodeEditor.BeginPin(GraphInputPin(node), PinKind.Input);
        NodeEditor.PinPivotAlignment(new Vector2(0f, 0.5f));
        NodeEditor.PinPivotSize(new Vector2(0, 0));
        VectorIcons.Icon(new Vector2(14), VectorIcon.Diamond, false, Color4.Green);
        ImGui.SameLine();
        ImGui.Text("In");
        NodeEditor.EndPin();

        ImGui.TableNextColumn();
        ImGui.TextWrapped(Shorten(GraphHeadline(node), 92));
        ImGui.TextDisabled($"line {node.Line}");
        foreach (var line in GraphCardLines(node).Take(5))
            ImGui.TextDisabled(Shorten(line, 86));

        ImGui.TableNextColumn();
        NodeEditor.BeginPin(GraphOutputPin(node), PinKind.Output);
        NodeEditor.PinPivotAlignment(new Vector2(1f, 0.5f));
        NodeEditor.PinPivotSize(new Vector2(0, 0));
        ImGui.Text("Out");
        ImGui.SameLine();
        VectorIcons.Icon(new Vector2(14), VectorIcon.Diamond, false, Color4.Green);
        NodeEditor.EndPin();
        ImGui.EndTable();

        DrawNodeBadges(node);

        ImGui.PopID();
        NodeEditor.EndNode();
        NodeEditor.PopStyleVar();

        if (dim)
            ImGui.PopStyleVar();
    }

    private void DrawNodeBadges(VignetteGraphNode node)
    {
        var broken = node.Outgoing.Count(x => x.Broken);
        if (broken == 0 && node.Diagnostics.Count == 0)
            return;
        ImGui.Separator();
        if (broken > 0)
            ImGui.TextColored(new Vector4(0.95f, 0.35f, 0.28f, 1), $"{broken} broken ref(s)");
        if (node.Diagnostics.Count > 0)
            ImGui.TextColored(new Vector4(1f, 0.72f, 0.25f, 1), $"{node.Diagnostics.Count} diagnostic(s)");
    }

    private void SyncGraphSelection()
    {
        if (NodeEditor.HasSelectionChanged())
        {
            Span<NodeId> nodes = stackalloc NodeId[Math.Max(1, graph.Nodes.Count)];
            var count = NodeEditor.GetSelectedNodes(nodes);
            if (count > 0)
            {
                var selectedId = nodes[0];
                var node = graph.Nodes.FirstOrDefault(x => GraphNodeId(x) == selectedId);
                if (node != null)
                    selected = node;
            }
        }

        var doubleClicked = NodeEditor.GetDoubleClickedNode();
        if (doubleClicked != 0)
        {
            var node = graph.Nodes.FirstOrDefault(x => GraphNodeId(x) == doubleClicked);
            if (node != null)
                selected = node;
        }
    }

    private void QueueGraphLayout()
    {
        graphPositions.Clear();
        var byId = graph.Nodes.ToDictionary(x => x.Id);
        var depth = new Dictionary<int, int>();
        var queue = new Queue<VignetteGraphNode>();

        foreach (var root in graph.Nodes.Where(x => x.IsRoot).OrderBy(x => x.Id))
        {
            depth[root.Id] = 0;
            queue.Enqueue(root);
        }
        if (queue.Count == 0 && graph.Nodes.FirstOrDefault() is { } first)
        {
            depth[first.Id] = 0;
            queue.Enqueue(first);
        }

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var nextDepth = depth[node.Id] + 1;
            foreach (var edge in node.Outgoing)
            {
                if (!byId.TryGetValue(edge.TargetNodeId, out var child))
                    continue;
                if (depth.TryGetValue(child.Id, out var existing) && existing <= nextDepth)
                    continue;
                depth[child.Id] = nextDepth;
                queue.Enqueue(child);
            }
        }

        var maxDepth = depth.Count == 0 ? 0 : depth.Values.Max();
        foreach (var node in graph.Nodes.Where(x => !depth.ContainsKey(x.Id)).OrderBy(x => x.Id))
            depth[node.Id] = ++maxDepth;

        var orderedLayers = BuildOrderedLayoutLayers(depth);
        foreach (var group in orderedLayers.OrderBy(x => x.Key))
        {
            var ordered = group.Value;
            var totalHeight = (ordered.Length - 1) * 190f;
            for (int i = 0; i < ordered.Length; i++)
            {
                var pos = new Vector2(group.Key * 460f, i * 190f - totalHeight * 0.5f);
                graphPositions.Enqueue((GraphNodeId(ordered[i]), pos));
            }
        }
        graphNavigateToSelected = true;
    }

    private Dictionary<int, VignetteGraphNode[]> BuildOrderedLayoutLayers(Dictionary<int, int> depth)
    {
        var layers = graph.Nodes
            .GroupBy(x => depth[x.Id])
            .ToDictionary(
                x => x.Key,
                x => x.OrderBy(n => n.Line).ThenBy(n => n.Index).ToArray());
        var maxDepth = layers.Count == 0 ? 0 : layers.Keys.Max();
        var previousOrder = new Dictionary<int, float>();

        for (var layer = 0; layer <= maxDepth; layer++)
        {
            if (!layers.TryGetValue(layer, out var nodes))
                continue;
            if (layer > 0)
            {
                nodes = nodes
                    .OrderBy(n => ParentMedian(n, previousOrder))
                    .ThenBy(n => n.Line)
                    .ThenBy(n => n.Index)
                    .ToArray();
                layers[layer] = nodes;
            }
            previousOrder = nodes
                .Select((node, index) => (node, index))
                .ToDictionary(x => x.node.Id, x => (float)x.index);
        }

        for (var layer = maxDepth - 1; layer >= 0; layer--)
        {
            if (!layers.TryGetValue(layer, out var nodes))
                continue;
            var childOrder = layers.TryGetValue(layer + 1, out var next)
                ? next.Select((node, index) => (node, index)).ToDictionary(x => x.node.Id, x => (float)x.index)
                : new Dictionary<int, float>();
            nodes = nodes
                .OrderBy(n => ChildMedian(n, childOrder))
                .ThenBy(n => n.Line)
                .ThenBy(n => n.Index)
                .ToArray();
            layers[layer] = nodes;
        }

        return layers;
    }

    private static float ParentMedian(VignetteGraphNode node, Dictionary<int, float> previousOrder)
    {
        var positions = node.Incoming
            .Where(x => previousOrder.ContainsKey(x.SourceNodeId))
            .Select(x => previousOrder[x.SourceNodeId])
            .OrderBy(x => x)
            .ToArray();
        return MedianOrMax(positions);
    }

    private static float ChildMedian(VignetteGraphNode node, Dictionary<int, float> childOrder)
    {
        var positions = node.Outgoing
            .Where(x => !x.Broken && childOrder.ContainsKey(x.TargetNodeId))
            .Select(x => childOrder[x.TargetNodeId])
            .OrderBy(x => x)
            .ToArray();
        return MedianOrMax(positions);
    }

    private static float MedianOrMax(float[] values)
    {
        if (values.Length == 0)
            return float.MaxValue;
        return values[values.Length / 2];
    }

    private static NodeId GraphNodeId(VignetteGraphNode node) => 100000 + node.Index + 1;
    private static PinId GraphInputPin(VignetteGraphNode node) => 200000 + node.Index + 1;
    private static PinId GraphOutputPin(VignetteGraphNode node) => 300000 + node.Index + 1;
    private static LinkId GraphLinkId(int edgeIndex) => 400000 + edgeIndex + 1;

    private static Color4 NodeColor(VignetteGraphNode node) =>
        node.Outgoing.Any(x => x.Broken) ? new Color4(150, 50, 42, 255) :
        node.IsUnreachable ? new Color4(94, 90, 105, 255) :
        node.Kind switch
        {
            VignetteNodeKind.Decision => new Color4(45, 105, 158, 255),
            VignetteNodeKind.Data => new Color4(55, 125, 70, 255),
            VignetteNodeKind.Documentation => new Color4(120, 95, 45, 255),
            _ => new Color4(110, 65, 110, 255)
        };

    private static VertexDiffuse EdgeColor(VignetteGraphEdge edge) =>
        edge.Relation.StartsWith("true", StringComparison.OrdinalIgnoreCase)
            ? (VertexDiffuse)new Color4(70, 170, 250, 255)
            : edge.Relation.StartsWith("false", StringComparison.OrdinalIgnoreCase)
                ? (VertexDiffuse)new Color4(245, 170, 70, 255)
                : edge.Relation.StartsWith("branch", StringComparison.OrdinalIgnoreCase)
                    ? (VertexDiffuse)new Color4(185, 130, 245, 255)
                    : (VertexDiffuse)new Color4(80, 190, 115, 255);

    private static string Shorten(string text, int max) =>
        text.Length <= max ? text : text[..Math.Max(0, max - 3)] + "...";

    private string GraphHeadline(VignetteGraphNode node)
    {
        if (showFactionNames)
        {
            var groupHeadline = GroupHeadline(node);
            if (groupHeadline != null)
                return groupHeadline;
        }

        var resolved = showIdsText ? ResolvedTextLines(node).FirstOrDefault() : null;
        if (resolved == null)
            return node.Summary;
        var colon = resolved.IndexOf(':');
        return colon >= 0 && colon + 1 < resolved.Length
            ? $"{node.Summary} -> {resolved[(colon + 1)..].Trim()}"
            : $"{node.Summary} -> {resolved}";
    }

    private IEnumerable<string> GraphCardLines(VignetteGraphNode node)
    {
        if (showFlowHints)
        {
            foreach (var line in FlowLines(node).Take(2))
                yield return line;
        }
        foreach (var line in HumanSemanticLines(node).Where(IsFactionSemanticLine).Take(2))
            yield return line;
        if (showIdsText)
        {
            foreach (var line in ResolvedTextLines(node).Take(2))
                yield return line;
        }
        foreach (var line in HumanSemanticLines(node).Where(x => !IsFlowSemanticLine(x) && !IsFactionSemanticLine(x)).Take(3))
            yield return line;
    }

    private static bool IsFlowSemanticLine(string line) =>
        line.StartsWith("branch match ->", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("true/left:", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("next:", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("children:", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("no child nodes", StringComparison.OrdinalIgnoreCase);

    private static bool IsFactionSemanticLine(string line) =>
        line.StartsWith("offer groups:", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("hostile groups:", StringComparison.OrdinalIgnoreCase);

    private string? GroupHeadline(VignetteGraphNode node)
    {
        if (node.FirstValue("offer_group") is { } offer)
            return $"offer groups: {HumanizeFactionList(offer)}";
        if (node.FirstValue("hostile_group") is { } hostile)
            return $"hostile groups: {HumanizeFactionList(hostile)}";
        return null;
    }

    private IEnumerable<string> FlowLines(VignetteGraphNode node)
    {
        if (node.IsRoot)
            yield return "entry point: no incoming child_node";
        foreach (var incoming in node.Incoming.OrderBy(x => x.SourceNodeId).Take(4))
            yield return $"called by #{incoming.SourceNodeId} via {incoming.Relation}";
        if (node.Incoming.Count > 4)
            yield return $"called by {node.Incoming.Count - 4} more node(s)";

        if (node.IsTerminal)
            yield return "terminal: no outgoing child_node";
        foreach (var outgoing in node.Outgoing.OrderBy(x => x.TargetNodeId).Take(4))
        {
            var target = graph.NodeById(outgoing.TargetNodeId);
            var targetName = target == null ? "missing node" : target.DisplayName;
            var suffix = outgoing.Broken ? " broken" : "";
            yield return $"{outgoing.Relation}: #{outgoing.TargetNodeId} {targetName}{suffix}";
        }
        if (node.Outgoing.Count > 4)
            yield return $"{node.Outgoing.Count - 4} more outgoing child_node link(s)";
    }

    private IEnumerable<string> ResolvedTextLines(VignetteGraphNode node)
    {
        foreach (var property in node.Properties)
        {
            if (!IsIdsTextProperty(property.Name))
                continue;
            foreach (var part in SplitCsv(property.Value))
            {
                if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                    continue;
                var text = ResolveStringId(id);
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                yield return $"{property.Name} IDS_NAME {id}: {Shorten(NormalizeResolvedText(text), 180)}";
            }
        }
    }

    private IEnumerable<string> HumanSemanticLines(VignetteGraphNode node)
    {
        foreach (var line in node.SemanticLines())
            yield return HumanizeSemanticLine(line);
    }

    private string HumanizeSemanticLine(string line)
    {
        if (!showFactionNames)
            return line;

        const string offerGroups = "offer groups:";
        const string hostileGroups = "hostile groups:";
        if (line.StartsWith(offerGroups, StringComparison.OrdinalIgnoreCase))
            return $"{offerGroups} {HumanizeFactionList(line[offerGroups.Length..])}";
        if (line.StartsWith(hostileGroups, StringComparison.OrdinalIgnoreCase))
            return $"{hostileGroups} {HumanizeFactionList(line[hostileGroups.Length..])}";
        return line;
    }

    private string HumanizeFactionList(string value)
    {
        return string.Join(", ", SplitCsv(value).Select(HumanizeFactionNickname));
    }

    private string HumanizeFactionNickname(string nickname)
    {
        if (nickname.Equals("all", StringComparison.OrdinalIgnoreCase))
            return "all";
        var faction = context.GameData.Items.Factions.Get(nickname);
        if (faction == null)
            return nickname;
        var name = context.GameData.GetString(faction.IdsName);
        if (string.IsNullOrWhiteSpace(name))
            return nickname;
        return $"{nickname} ({NormalizeResolvedText(name)})";
    }

    private string BuildTreeText()
    {
        try
        {
            var vparams = new VignetteParamsIni();
            vparams.AddFile(sourcePath, context.GameData.VFS);
            return AnnotateTreeText(VignetteParamsDecompiler.Decompile(vparams));
        }
        catch (Exception ex)
        {
            return BuildGraphTreeText(ex);
        }
    }

    private string AnnotateTreeText(string text)
    {
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var groups = ExtractGroupDefinitions(lines);
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            sb.AppendLine(line);
            var hint = TreeHintForLine(line.Trim(), groups);
            if (hint == null)
                continue;
            var indent = line.Length - line.TrimStart().Length;
            sb.Append(' ', indent);
            sb.Append("# ");
            sb.AppendLine(hint);
        }
        return sb.ToString();
    }

    private static Dictionary<string, string> ExtractGroupDefinitions(IEnumerable<string> lines)
    {
        var groups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines.Select(x => x.Trim()))
        {
            if (!line.StartsWith("group ", StringComparison.OrdinalIgnoreCase))
                continue;
            var body = line["group ".Length..].TrimEnd(';').Trim();
            var nameEnd = body.IndexOf(' ');
            if (nameEnd <= 0)
                continue;
            groups[body[..nameEnd]] = body[(nameEnd + 1)..].Trim();
        }
        return groups;
    }

    private string? TreeHintForLine(string line, Dictionary<string, string> groups)
    {
        if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            return null;
        if (line.StartsWith("group ", StringComparison.OrdinalIgnoreCase))
        {
            var body = line["group ".Length..].TrimEnd(';').Trim();
            var nameEnd = body.IndexOf(' ');
            if (nameEnd <= 0)
                return null;
            return $"defines {body[..nameEnd]}: {HumanizeFactionGroup(body[(nameEnd + 1)..])}";
        }
        if (line.StartsWith("offer_group ", StringComparison.OrdinalIgnoreCase))
            return GroupReferenceHint("offer factions", line["offer_group ".Length..], groups);
        if (line.StartsWith("hostile_group ", StringComparison.OrdinalIgnoreCase))
            return GroupReferenceHint("hostile factions", line["hostile_group ".Length..], groups);
        if (line.StartsWith("if group", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("elif group", StringComparison.OrdinalIgnoreCase))
        {
            var start = line.IndexOf('(');
            var end = line.IndexOf(')');
            if (start >= 0 && end > start)
                return GroupReferenceHint("condition factions", line[(start + 1)..end], groups);
        }
        if (line.StartsWith("objective_text ", StringComparison.OrdinalIgnoreCase))
            return IdTextHint("objective_text", line);
        if (line.StartsWith("reward_text ", StringComparison.OrdinalIgnoreCase))
            return IdTextHint("reward_text", line);
        if (line.StartsWith("failure_text ", StringComparison.OrdinalIgnoreCase))
            return IdTextHint("failure_text", line);
        if (line.Contains('(') && line.Contains(')') &&
            (line.StartsWith("append", StringComparison.OrdinalIgnoreCase) ||
             line.StartsWith("replace", StringComparison.OrdinalIgnoreCase)))
        {
            return IdTextHint("offer_text", line);
        }
        return null;
    }

    private string? GroupReferenceHint(string prefix, string groupNameText, Dictionary<string, string> groups)
    {
        var groupName = groupNameText.Trim().TrimEnd(';').Trim();
        if (!groups.TryGetValue(groupName, out var factions))
            return null;
        return $"{prefix}: {HumanizeFactionGroup(factions)}";
    }

    private string HumanizeFactionGroup(string factions)
    {
        var parts = SplitCsv(factions);
        if (parts.Length == 0)
            return "empty group";
        var rendered = parts.Select(HumanizeFactionNickname).Take(10).ToArray();
        var suffix = parts.Length > rendered.Length ? $" ... +{parts.Length - rendered.Length} more" : "";
        return string.Join(", ", rendered) + suffix;
    }

    private string? IdTextHint(string label, string line)
    {
        var ids = SplitIdsFromCodeLine(line);
        if (ids.Length == 0)
            return null;
        var texts = ids
            .Select(id => (id, text: ResolveStringId(id)))
            .Where(x => !string.IsNullOrWhiteSpace(x.text))
            .Take(3)
            .Select(x => $"IDS_NAME {x.id}: {Shorten(NormalizeResolvedText(x.text!), 120)}")
            .ToArray();
        return texts.Length == 0 ? null : $"{label}: {string.Join("; ", texts)}";
    }

    private static int[] SplitIdsFromCodeLine(string line)
    {
        var ids = new List<int>();
        var current = new StringBuilder();
        foreach (var ch in line)
        {
            if (char.IsDigit(ch))
            {
                current.Append(ch);
                continue;
            }
            Flush();
        }
        Flush();
        return ids.ToArray();

        void Flush()
        {
            if (current.Length == 0)
                return;
            if (int.TryParse(current.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                ids.Add(id);
            current.Clear();
        }
    }

    private void CompileTreePreview()
    {
        try
        {
            var sections = VignetteParamsCompiler.Compile(treeText, "Vignette Tree");
            var ini = SerializeIniSections(sections);
            treeCompileStatus = $"Compiled {sections.Count} section(s).";
            win.TextWindows.Add(new TextDisplayWindow(ini, "vignetteparams.compiled.ini", win));
        }
        catch (Exception ex)
        {
            treeCompileStatus = $"Compile error: {ex.Message}";
        }
    }

    private static string SerializeIniSections(IEnumerable<Section> sections)
    {
        using var stream = new MemoryStream();
        IniWriter.WriteIni(stream, sections);
        return Encoding.GetEncoding(1252).GetString(stream.ToArray());
    }

    private string BuildGraphTreeText(Exception? decompileError)
    {
        var sb = new StringBuilder();
        if (decompileError != null)
        {
            sb.AppendLine("Decompiler could not build the high-level if/elif/else view.");
            sb.AppendLine($"Fallback structural tree is shown from raw child_node links. {decompileError.GetType().Name}: {decompileError.Message}");
            sb.AppendLine();
        }

        sb.AppendLine(sourcePath);
        sb.AppendLine();
        var emitted = new HashSet<int>();
        var roots = graph.Nodes.Where(x => x.IsRoot).OrderBy(x => x.Line).ThenBy(x => x.Id).ToArray();
        if (roots.Length == 0)
            roots = graph.Nodes.OrderBy(x => x.Line).ThenBy(x => x.Id).Take(1).ToArray();

        foreach (var root in roots)
            AppendTreeNode(sb, root, 0, emitted, []);

        var remaining = graph.Nodes
            .Where(x => !emitted.Contains(x.Id))
            .OrderBy(x => x.Line)
            .ThenBy(x => x.Id)
            .ToArray();
        if (remaining.Length == 0)
            return sb.ToString();

        sb.AppendLine();
        sb.AppendLine("Detached or shared nodes:");
        foreach (var node in remaining)
            AppendTreeNode(sb, node, 0, emitted, []);
        return sb.ToString();
    }

    private void AppendTreeNode(StringBuilder sb, VignetteGraphNode node, int indent, HashSet<int> emitted, HashSet<int> path)
    {
        AppendIndent(sb, indent);
        sb.Append(node.DisplayName);
        sb.Append("  ");
        sb.Append(GraphHeadline(node));
        sb.Append("  line ");
        sb.Append(node.Line);
        if (node.IsTerminal)
            sb.Append("  terminal");
        if (node.IsUnreachable)
            sb.Append("  unreachable");
        sb.AppendLine();

        if (!path.Add(node.Id))
        {
            AppendIndent(sb, indent + 1);
            sb.AppendLine("cycle: node is already in this path");
            return;
        }

        emitted.Add(node.Id);
        foreach (var line in HumanSemanticLines(node).Where(x => !IsFlowSemanticLine(x)).Take(6))
        {
            AppendIndent(sb, indent + 1);
            sb.Append("- ");
            sb.AppendLine(line);
        }
        if (showIdsText)
        {
            foreach (var line in ResolvedTextLines(node).Take(4))
            {
                AppendIndent(sb, indent + 1);
                sb.Append("- ");
                sb.AppendLine(line);
            }
        }

        foreach (var edge in OrderedOutgoing(node))
        {
            AppendIndent(sb, indent + 1);
            sb.Append("-> ");
            sb.Append(edge.Relation);
            sb.Append(": #");
            sb.Append(edge.TargetNodeId);
            if (edge.Broken)
            {
                sb.AppendLine(" missing");
                continue;
            }
            var target = graph.NodeById(edge.TargetNodeId);
            if (target == null)
            {
                sb.AppendLine(" missing");
                continue;
            }
            if (path.Contains(target.Id))
            {
                sb.AppendLine(" cycle");
                continue;
            }
            if (emitted.Contains(target.Id))
            {
                sb.Append(" ref ");
                sb.Append(target.DisplayName);
                sb.Append(" line ");
                sb.AppendLine(target.Line.ToString(CultureInfo.InvariantCulture));
                continue;
            }

            sb.AppendLine();
            AppendTreeNode(sb, target, indent + 2, emitted, new HashSet<int>(path));
        }
    }

    private static IEnumerable<VignetteGraphEdge> OrderedOutgoing(VignetteGraphNode node) =>
        node.Outgoing
            .Select((edge, index) => (edge, index, childIndex: ChildIndex(node, edge.TargetNodeId)))
            .OrderBy(x => x.childIndex < 0 ? int.MaxValue : x.childIndex)
            .ThenBy(x => x.index)
            .Select(x => x.edge);

    private static int ChildIndex(VignetteGraphNode node, int childId)
    {
        for (var i = 0; i < node.ChildIds.Count; i++)
        {
            if (node.ChildIds[i] == childId)
                return i;
        }
        return -1;
    }

    private static void AppendIndent(StringBuilder sb, int indent) =>
        sb.Append(' ', indent * 2);

    private string? ResolveStringId(int id)
    {
        try
        {
            return context.Infocards.GetStringResource(id);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsIdsTextProperty(string name) =>
        name.Equals("offer_text", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("objective_text", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Reward_text", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Failure_text", StringComparison.OrdinalIgnoreCase);

    private static string[] SplitCsv(string value) =>
        value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static string NormalizeResolvedText(string value) =>
        value.Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Trim();

    private string BuildDiagnosticExport()
    {
        var sb = new StringBuilder();
        sb.AppendLine(graph.SourcePath);
        if (!string.IsNullOrWhiteSpace(graph.BackingPath))
            sb.AppendLine(graph.BackingPath);
        sb.AppendLine($"Nodes: {graph.Nodes.Count}");
        sb.AppendLine($"Edges: {graph.Edges.Count}");
        sb.AppendLine();
        foreach (var diagnostic in graph.Diagnostics)
            sb.AppendLine($"{diagnostic.Severity} {(diagnostic.NodeId is { } id ? $"node {id}" : "global")}: {diagnostic.Message}");
        return sb.ToString();
    }

    public override void Dispose()
    {
        treeEditor.Dispose();
        graphContext.Dispose();
        graphConfig.Dispose();
        base.Dispose();
    }
}
