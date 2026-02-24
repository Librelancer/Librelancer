using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;
using LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Data;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using ImGui = ImGuiNET.ImGui;

namespace LancerEdit.GameContent.MissionEditor;

public sealed partial class MissionScriptEditorTab : GameContentTab
{
    private readonly GameDataContext gameData;
    private MainWindow win;
    private PopupManager popup;

    private readonly NodeEditorConfig config;
    private readonly NodeEditorContext context;
    private readonly List<Node> nodes;
    private int nextId;
    private NodeId contextNodeId = 0;
    private readonly Queue<(NodeId Id, Vector2 Pos)> nodeRelocationQueue = [];

    private readonly MissionScriptDocument missionIni;
    public string FileSaveLocation;
    public string NodeFilter = "";

    EditorUndoBuffer undoBuffer = new();

    private bool renderHistory = false;

    public MissionScriptEditorTab(GameDataContext gameData, MainWindow win, string file)
    {
        Title = $"Mission Script Editor - {Path.GetFileName(file)}";
        DocumentName = Path.GetFileName(file);
        FileSaveLocation = file;
        this.gameData = gameData;
        this.win = win;
        popup = new PopupManager();

        config = new NodeEditorConfig();
        config.SettingsFile = null;
        config.SetNodeDraggedHook(OnNodeDragged);
        config.SetNodeResizedHook(OnNodeResized);
        context = new NodeEditorContext(config);
        SaveStrategy = new MissionSaveStrategy(win, this);

        NodeBuilder.LoadTexture(win.RenderContext);

        nodes = [];

        // Load ini file
        missionIni = MissionScriptDocument.FromFile(file, gameData, out var iniTriggers);

        Queue<(Node Source, string Target, bool Input)> toLink = new();

        var triggerNodes = new Dictionary<string, NodeMissionTrigger>(StringComparer.OrdinalIgnoreCase);
        Dictionary<NodeMissionTrigger, int> counts = new Dictionary<NodeMissionTrigger, int>();


        foreach (var t in iniTriggers)
        {
            var n = new NodeMissionTrigger(t, this);
            triggerNodes[t.Nickname] = n;
            nodes.Add(n);
            counts[n] = 0;
            foreach (var c in n.Actions)
            {
                if (c is ActActivateNodeTrigger or ActDeactivateNodeTrigger or ActSave)
                {
                    var triggerTarget = c switch
                    {
                        ActActivateNodeTrigger act => act.Data.Trigger,
                        ActDeactivateNodeTrigger deactivate => deactivate.Data.Trigger,
                        ActSave save => save.Data.Trigger,
                        _ => throw new InvalidCastException()
                    };
                    toLink.Enqueue((c, triggerTarget, false));
                }
            }

            foreach (var c in n.Conditions)
            {
                if (c is CndWatchNodeTrigger watch)
                {
                    toLink.Enqueue((watch, watch.Data.Trigger, true));
                }
            }
        }

        while (toLink.Count > 0)
        {
            var x = toLink.Dequeue();
            if (!triggerNodes.TryGetValue(x.Target, out var target))
            {
                // warning
                continue;
            }

            if (x.Input)
            {
                TryLinkNodes(target, x.Source, LinkType.Trigger, out _);
            }
            else
            {
                TryLinkNodes(x.Source, target, LinkType.Trigger, out _);
            }

            counts[target]++;
        }

        if (!ReadSavedPositions(file))
        {
            AutoPositionNodes(triggerNodes, counts);
        }

        SetupLookups();
    }

    bool ReadSavedPositions(string file)
    {
        using var fileStream = File.OpenRead(file);
        var sections = IniFile.ParseFile(file, fileStream);
        var nodesSection = sections.FirstOrDefault(x => x.Name.Equals("nodes", StringComparison.OrdinalIgnoreCase));
        if (nodesSection == null)
            return false;
        int count = 0;
        foreach (var nodePos in nodesSection)
        {
            if (nodePos.Count < 4)
            {
                FLLog.Warning("MissionScriptEditor", "Invalid node position in nodes section!");
                continue;
            }

            var data = SavedNode.FromEntry(nodePos);

            if (data.IsTrigger)
            {
                var trigger = nodes.OfType<NodeMissionTrigger>().FirstOrDefault(x => x.Data.Nickname == data.Name);
                if (trigger is null)
                {
                    FLLog.Warning("MissionScriptEditor", $"Trigger '{data.Name}' in the nodes section was not found.");
                    continue;
                }

                trigger.IsCollapsed = data.IsCollapsed;
                nodeRelocationQueue.Enqueue((trigger.Id, data.Position));
                count++;
            }
            else
            {
                var comment = new CommentNode() { BlockName = data.Name };
                nodes.Add(comment);
                nodeRelocationQueue.Enqueue((comment.Id, data.Position));
                comment.SetGroupSize(data.Size);
            }
        }

        return count > 0;
    }

    void AutoPositionNodes(
        Dictionary<string, NodeMissionTrigger> triggerNodes,
        Dictionary<NodeMissionTrigger, int> counts)
    {
        var columns = new List<List<NodeMissionTrigger>>();

        HashSet<NodeMissionTrigger> setColumns = new HashSet<NodeMissionTrigger>();

        columns.Add(new List<NodeMissionTrigger>());

        void IterateChildren(NodeMissionTrigger triggerNode, int index)
        {
            while (columns.Count <= index)
                columns.Add(new List<NodeMissionTrigger>());
            foreach (var c in triggerNode.Actions)
            {
                if (c is ActActivateNodeTrigger or ActDeactivateNodeTrigger or ActSave)
                {
                    var triggerTarget = c switch
                    {
                        ActActivateNodeTrigger act => act.Data.Trigger,
                        ActDeactivateNodeTrigger deactivate => deactivate.Data.Trigger,
                        ActSave save => save.Data.Trigger,
                        _ => throw new InvalidCastException()
                    };
                    if (!triggerNodes.TryGetValue(triggerTarget, out var target))
                    {
                        continue;
                    }

                    if (!setColumns.Contains(target))
                    {
                        setColumns.Add(target);
                        columns[index].Add(target);
                        IterateChildren(target, index + 1);
                    }
                }
            }
        }

        foreach (var startNode in counts.Where(x => x.Value == 0)
                     .Select(x => x.Key))
        {
            setColumns.Add(startNode);
            columns[0].Add(startNode);
            IterateChildren(startNode, 1);
        }

        for (int i = 0; i < columns.Count; i++)
        {
            float totalHeight = 0;
            foreach (var item in columns[i])
                totalHeight += item.EstimateHeight();
            float cHeight = -(totalHeight / 2);
            for (int j = 0; j < columns[i].Count; j++)
            {
                var x = (i * 1000);
                nodeRelocationQueue.Enqueue((columns[i][j].Id, new Vector2(x, cHeight)));
                cHeight += columns[i][j].EstimateHeight();
            }
        }
    }

    public override void OnHotkey(Hotkeys hk, bool shiftPressed)
    {
        switch (hk)
        {
            case Hotkeys.Undo when undoBuffer.CanUndo:
                undoBuffer.Undo();
                break;
            case Hotkeys.Redo when undoBuffer.CanRedo:
                undoBuffer.Redo();
                break;
        }
    }

    public override void Draw(double elapsed)
    {
        //ImGuiHelper.AnimatingElement();
        if (!ImGui.BeginTable("ME Table", 3, ImGuiTableFlags.None))
        {
            return;
        }

        ImGui.TableSetupColumn("ME Left Sidebar", ImGuiTableColumnFlags.WidthFixed, 300f * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("ME Node Editor", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("ME Right Sidebar", ImGuiTableColumnFlags.WidthFixed, 300f * ImGuiHelper.Scale);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        RenderLeftSidebar();

        ImGui.TableNextColumn();
        RenderNodeEditor();

        ImGui.TableNextColumn();
        RenderRightSidebar();

        ImGui.EndTable();

        popup.Run();
        if (renderHistory)
            undoBuffer.DisplayStack();
    }


    public EditorAction OnRenameTrigger(NodeMissionTrigger node, string oldName, string newName) =>
         new RenameTriggerAction(node, this, oldName, newName);

    private Queue<Action> nodeEditActions = new();


    private NodeMissionTrigger jumpToNode = null;

    private void RenderNodeEditor()
    {
        NodeEditor.SetCurrentEditor(context);
        NodeEditor.Begin("Node Editor", Vector2.Zero);

        var cursorTopLeft = ImGui.GetCursorScreenPos();

        while (nodeEditActions.Count > 0)
        {
            nodeEditActions.Dequeue()();
        }

        while (nodeRelocationQueue.Count > 0)
        {
            var node = nodeRelocationQueue.Dequeue();
            NodeEditor.SetNodePosition(node.Id, node.Pos);
        }

        var lookups = new NodeLookups() { MissionIni = missionIni };
        foreach (var node in nodes)
        {
            var isFiltered = NodeFilter is not "" &&
                             node.InternalId.IndexOf(NodeFilter, StringComparison.OrdinalIgnoreCase) == -1;

            if (isFiltered)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.3f);
            }

            node.Render(gameData, popup, undoBuffer, ref lookups);

            if (isFiltered)
            {
                ImGui.PopStyleVar();
            }
        }

        foreach (var link in NodePin.AllLinks)
        {
            NodeEditor.Link(link.Id, link.StartPin.Id, link.EndPin.Id, link.Color, 2.0f);
        }

        TryCreateLink();
        TryDeleteLink();

        if (jumpToNode != null)
        {
            NodeEditor.ClearSelection();
            NodeEditor.SelectNode(jumpToNode.Id);
            NodeEditor.NavigateToSelection(true);
            jumpToNode = null;
        }

        ImGui.SetCursorScreenPos(cursorTopLeft);

        var contextPos = ImGui.GetMousePos();
        NodeEditor.Suspend();
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));

        const string nodeContextMenu = "Node Context Menu";
        const string newNodeContextMenu = "Create New Node";
        const string linkContextMenu = "Link Context Menu";

        if (NodeEditor.ShowNodeContextMenu(out var foundNode))
        {
            if (foundNode != 0)
            {
                contextNodeId = foundNode;

                // If the node isn't already selected,clear selection and select it.
                if (!NodeEditor.IsNodeSelected(foundNode))
                {
                    NodeEditor.ClearSelection();
                    NodeEditor.SelectNode(foundNode);
                }
            }

            ImGui.OpenPopup(nodeContextMenu);
        }
        else if (NodeEditor.ShowLinkContextMenu(out var linkNodeId))
        {
            ImGui.OpenPopup(linkContextMenu);
        }
        else if (NodeEditor.ShowBackgroundContextMenu())
        {
            ImGui.OpenPopup(newNodeContextMenu);
        }

        if (ImGui.BeginPopup(nodeContextMenu))
        {
            NodeContextMenu();
            ImGui.EndPopup();
        }

        if (ImGui.BeginPopup(newNodeContextMenu))
        {
            CreateNewNodeContextMenu(contextPos);
            ImGui.EndPopup();
        }

        ImGui.PopStyleVar();
        NodeEditor.Resume();
        NodeEditor.End();
        NodeEditor.SetCurrentEditor(null);

        if (nodeMouseActions.Count > 0)
        {
            undoBuffer.Push(EditorAggregateAction.Create(nodeMouseActions.ToArray()));
            nodeMouseActions = new();
        }

        // Works around rare assert
        ImGui.SameLine();
        ImGui.Dummy(new Vector2(0));
    }

    private List<EditorAction> nodeMouseActions = new();

    // Dragging a comment node fires an event per moved node. Queue up for aggregate
    private void OnNodeDragged(NodeId nodeId, float oldX, float oldY, float newX, float newY, IntPtr userPointer)
    {
        nodeMouseActions.Add(new MoveNodeAction(nodeId, new(oldX, oldY), new(newX, newY), this));
    }

    private void OnNodeResized(NodeId nodeId, ref ResizeCallbackData data, IntPtr userPointer)
    {
        if (data.StartPosition != data.EndPosition)
        {
            nodeMouseActions.Add(new MoveNodeAction(nodeId, data.StartPosition, data.EndPosition, this));
        }

        if (data.StartGroupSize != data.EndGroupSize)
        {
            var act = new SetNodeGroupSize(nodeId, data.StartGroupSize, data.EndGroupSize, this);
            act.Set(data.EndGroupSize); // update C#-side value
            nodeMouseActions.Add(act);
        }
    }


    private bool TryLinkNodes(Node start, Node end, LinkType linkType, out NodeLink link, LinkId? setId = null)
    {
        link = null;
        var startPin = start.Outputs.FirstOrDefault(x => x.LinkType == linkType);
        if (startPin is null)
        {
            return false;
        }

        var endPin = end.Inputs.FirstOrDefault(x => x.LinkType == linkType);
        if (endPin is null)
        {
            return false;
        }

        link = startPin.CreateLink(endPin, null, setId);
        return link != null;
    }

    List<(Node Start, Node End, LinkType LinkType, LinkId LinkId)> GetLinks(Node node)
    {
        var allLinks = new List<(Node Start, Node End, LinkType LinkType, LinkId LinkId)>();
        foreach (var l in node.Outputs)
        {
            foreach (var link in l.Links)
            {
                allLinks.Add((node, link.EndPin.OwnerNode, l.LinkType, link.Id));
            }
        }

        foreach (var i in node.Inputs)
        {
            foreach (var link in i.Links)
            {
                allLinks.Add((link.StartPin.OwnerNode, node, i.LinkType, link.Id));
            }
        }

        return allLinks;
    }

    private void TryDeleteLink()
    {
        if (NodeEditor.BeginDelete())
        {
            if (NodeEditor.QueryDeletedLink(out var linkId) && NodeEditor.AcceptDeletedItem())
            {
                var toDelete = NodePin.AllLinks.FirstOrDefault(x => x.Id == linkId);
                if (toDelete != null)
                {
                    undoBuffer.Commit(new DeleteLinkAction(toDelete));
                }
            }
        }

        NodeEditor.EndDelete();
    }

    private void TryCreateLink()
    {
        if (!NodeEditor.BeginCreate(Color4.White, 2.0f))
        {
            NodeEditor.EndCreate();
            return;
        }

        if (NodeEditor.QueryNewLink(out var startPinId, out var endPinId))
        {
            var startPin = FindPin(startPinId);
            var endPin = FindPin(endPinId);

            Debug.Assert(startPin != null, nameof(startPin) + " != null");

            if (startPin.PinKind == PinKind.Input)
            {
                // Swap pins
                (startPin, endPin) = (endPin, startPin);
            }

            // If we are dragging a pin and hovering a pin, check if we can connect
            if (startPin is not null && endPin is not null)
            {
                if (startPin.CanCreateLink(endPin, ShowLabel))
                {
                    undoBuffer.Commit(new CreateLinkAction(startPin, endPin, this));
                }
            }
        }

        NodeEditor.EndCreate();
        return;

        void ShowLabel(string label, bool success)
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetTextLineHeight());
            var size = ImGui.CalcTextSize(label);

            var padding = ImGui.GetStyle().FramePadding;
            var spacing = ImGui.GetStyle().ItemSpacing;

            ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(spacing.X, -spacing.Y));

            var rectMin = ImGui.GetCursorScreenPos() - padding;
            var rectMax = ImGui.GetCursorScreenPos() + size + padding;

            var drawList = ImGui.GetWindowDrawList();
            var color = success ? new Color4(32, 45, 32, 180) : new Color4(45, 32, 32, 180);
            drawList.AddRectFilled(rectMin, rectMax, ImGui.ColorConvertFloat4ToU32(color), size.Y * 0.15f);
            ImGui.Text(label);
        }
    }


    private void CreateNewNodeContextMenu(Vector2 position)
    {
        ImGui.Text("Create New Node");
        ImGui.Separator();

        if (ImGui.MenuItem("Trigger"))
        {
            var c = NameInputConfig.Nickname("New Trigger", x => GetTrigger(x) != null);
            popup.OpenPopup(new NameInputPopup(c, "", x => undoBuffer.Commit(new NewTriggerAction(x, position, this))));
        }

        if (ImGui.MenuItem("Comment Node"))
        {
            undoBuffer.Commit(new NewCommentAction(position, this));
        }
    }

    private void NodeContextMenu()
    {
        Span<NodeId> nodeIds = stackalloc NodeId[nodes.Count];
        var selectedNodes = NodeEditor.GetSelectedNodes(nodeIds);

        var nodeId = nodeIds[0];
        var node = nodes.Find(x => x.Id == nodeId);
        if (node is null)
        {
            node = nodes.Find(x => x.Id == contextNodeId);
            if (node is null)
            {
                return;
            }

            selectedNodes = 1;
            nodeIds[0] = contextNodeId;
        }

        ImGui.Text("Node Context Menu");
        if (selectedNodes is 1)
        {
            ImGui.Separator();
            ImGui.Text($"ID: {node.Id}");
            ImGui.Text($"Type: {node.GetType().Name}");
            ImGui.Text($"Inputs: {node.Inputs.Count}");
            ImGui.Text($"Outputs: {node.Outputs.Count}");
        }

        ImGui.Separator();
        if (ImGui.MenuItem((selectedNodes is 1)? "Duplicate Node" : "Duplicate Nodes"))
        {
            DuplicateSelectedNodes();
            ImGui.CloseCurrentPopup();
            return;
        }
        if (!node.OnContextMenu(popup, undoBuffer))
        {
            return;
        }

        for (var index = 0; index != selectedNodes; index++)
        {
            nodeId = nodeIds[index];
            var nodeIdx = nodes.FindIndex(x => x.Id == nodeId);
            if (nodeIdx == -1)
            {
                continue;
            }

            undoBuffer.Commit(new DeleteNodeAction(nodes[nodeIdx].Id, node, this));
        }

        contextNodeId = 0;
    }

    NodePin FindIn(PinId id, Node node)
    {
        var pin = node.Inputs.FirstOrDefault(x => x.Id == id);
        if (pin is not null)
        {
            return pin;
        }

        pin = node.Outputs.FirstOrDefault(x => x.Id == id);
        if (pin is not null)
        {
            return pin;
        }

        if (node is NodeMissionTrigger trigger)
        {
            foreach (var cond in trigger.Conditions)
            {
                var r = FindIn(id, cond);
                if (r is not null)
                    return r;
            }

            foreach (var act in trigger.Actions)
            {
                var r = FindIn(id, act);
                if (r is not null)
                    return r;
            }
        }

        return null;
    }

    private NodePin FindPin(PinId id)
    {
        if (id == 0)
        {
            return null;
        }

        foreach (var node in nodes)
        {
            var r = FindIn(id, node);
            if (r is not null)
                return r;
        }

        return null;
    }

    public void DeleteCondition(NodeMissionTrigger trigger, int index)
    {
        undoBuffer.Commit(new DeleteConditionAction(trigger, index, this));
    }

    public void DeleteAction(NodeMissionTrigger trigger, int index)
    {
        undoBuffer.Commit(new DeleteActionAction(trigger, index, this));
    }

    public override void Dispose()
    {
        context.Dispose();
        config.Dispose();
        base.Dispose();
    }


    static bool SidebarHeader(string id)
    {
        ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.FrameBg));
        var r = ImGui.CollapsingHeader(id, ImGuiTreeNodeFlags.DefaultOpen);
        ImGui.PopStyleColor();
        return r;
    }

    void ItemList<T>(string itemName,
        SortedDictionary<string, T> items,
        FieldAccessor<T> selected,
        Func<DictionaryRemove<T>> getDeleter) where T : NicknameItem, new()
    {
        ref var selection = ref selected();
        ImGui.PushID(itemName);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeightWithSpacing() * 3);
        if (ImGui.BeginCombo($"##{itemName}", selection is not null ? selection.Nickname : ""))
        {
            foreach (var kv in items)
            {
                if (ImGui.Selectable(kv.Key, selection == kv.Value))
                {
                    selection = kv.Value;
                }
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        if (ImGui.Button(Icons.PlusCircle))
        {
            popup.OpenPopup(new NameInputPopup(
                NameInputConfig.Nickname(itemName, items.ContainsKey),
                "",
                x =>
                {
                    undoBuffer.Commit(new DictionaryAdd<T>(itemName, items, new T() { Nickname = x }, selected));
                }));
        }

        ImGui.SetItemTooltip($"Create New {itemName}");
        ImGui.SameLine();
        ImGui.BeginDisabled(selection == null);
        if (ImGui.Button(Icons.TrashAlt))
        {
            win.Confirm($"Are you sure you want to delete '{selection!.Nickname}'?",
                () => { undoBuffer.Commit(getDeleter()); });
        }

        ImGui.SetItemTooltip($"Delete {itemName}");
        ImGui.EndDisabled();
        ImGui.PopID();
    }

    internal EditResult<bool> SaveMission(string savePath = null)
    {
        if (savePath != null)
        {
            FileSaveLocation = savePath;
        }

        // Fetch locations from node editor state
        var savedNodes = new List<SavedNode>();
        NodeEditor.SetCurrentEditor(context);
        foreach (var trigger in nodes.OfType<NodeMissionTrigger>())
        {
            savedNodes.Add(SavedNode.FromTrigger(NodeEditor.GetNodePosition(trigger.Id), trigger));
        }

        foreach (var node in nodes.OfType<CommentNode>())
        {
            savedNodes.Add(SavedNode.FromComment(NodeEditor.GetNodePosition(node.Id), node));
        }

        NodeEditor.SetCurrentEditor(null);

        missionIni.Save(FileSaveLocation, gameData, nodes.OfType<NodeMissionTrigger>(), savedNodes);

        return new EditResult<bool>(true);
    }

    private void DuplicateSelectedNodes()
    {
        Span<NodeId> nodeIds = stackalloc NodeId[nodes.Count];
        var selectedCount = NodeEditor.GetSelectedNodes(nodeIds);

        if (selectedCount == 0)
            return;

        const float offset = 40f;

        var actions = new List<EditorAction>();

        for (int i = 0; i < selectedCount; i++)
        {
            Node original = null;
            var id = nodeIds[i];

            for (int j = 0; j < nodes.Count; j++)
            {
                if (nodes[j].Id == id)
                {
                    original = nodes[j];
                    break;
                }
            }

            if (original == null)
                continue;

            var clone = original.Clone(this);

            var pos = NodeEditor.GetNodePosition(original.Id);
            var newPos = pos + new Vector2(offset, offset);

            actions.Add(new NewNodeFromCloneAction(clone, newPos, this));
        }

        if (actions.Count > 0)
            undoBuffer.Commit(EditorAggregateAction.Create(actions.ToArray()));
    }

    internal string GenerateUniqueTriggerName(string baseName)
    {
        int i = 1;
        string newName;

        do
        {
            newName = $"{baseName}_Copy{i}";
            i++;
        }
        while (GetTrigger(newName) != null);

        return newName;
    }
    internal void AddNode(Node node, Vector2 pos)
    {
        nodes.Add(node);
        NodeEditor.SetNodePosition(node.Id, pos);
        SetupLookups();
    }

    internal void RemoveNode(Node node)
    {
        nodes.Remove(node);
        NodeEditor.DeleteNode(node.Id);
        SetupLookups();
    }
}
