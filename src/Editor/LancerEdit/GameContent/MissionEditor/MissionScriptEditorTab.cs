using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;
using LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;
using LancerEdit.GameContent.MissionEditor.Popups;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using Microsoft.EntityFrameworkCore.Query;
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
    private readonly List<NodeMissionTrigger> triggers = [];
    private int nextId;
    private NodeId contextNodeId = 0;
    private readonly Queue<(NodeId Id, Vector2 Pos)> nodeRelocationQueue = [];

    private readonly MissionIni missionIni;
    public string FileSaveLocation;
    public string NodeFilter = "";

    public MissionScriptEditorTab(GameDataContext gameData, MainWindow win, string file)
    {
        Title = $"Mission Script Editor - {Path.GetFileName(file)}";
        FileSaveLocation = file;
        this.gameData = gameData;
        this.win = win;
        popup = new PopupManager();

        config = new NodeEditorConfig();
        config.SettingsFile = null;
        context = new NodeEditorContext(config);
        SaveStrategy = new MissionSaveStrategy(win, this);

        NodeBuilder.LoadTexture(win.RenderContext);

        nodes = [];
        missionIni = new MissionIni(file, null);

        var npcPath = gameData.GameData.VFS.GetBackingFileName(gameData.GameData.DataPath(missionIni.Info.NpcShipFile));
        if (npcPath is not null)
        {
            missionIni.ShipIni = new NPCShipIni(npcPath, null);
        }

        var pos = new Vector2(0, 0);

        Queue<(Node Source, string Target, bool Input)> toLink = new();

        var triggerNodes = new Dictionary<string, NodeMissionTrigger>(StringComparer.OrdinalIgnoreCase);
        Dictionary<NodeMissionTrigger, int> counts = new Dictionary<NodeMissionTrigger, int>();


        foreach (var t in missionIni.Triggers)
        {
            var n = new NodeMissionTrigger(t);
            triggerNodes[t.Nickname] = n;
            nodes.Add(n);
            counts[n] = 0;
            foreach (var c in n.Actions) {
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
            var x= toLink.Dequeue();
            if (!triggerNodes.TryGetValue(x.Target, out var target)) {
                // warning
                continue;
            }
            if (x.Input)
            {
                TryLinkNodes(target, x.Source, LinkType.Trigger);
            }
            else
            {
                TryLinkNodes(x.Source, target, LinkType.Trigger);
            }
            counts[target]++;
        }

        if (!ReadSavedPositions(file))
        {
            AutoPositionNodes(triggerNodes, counts);
        }
    }

    bool ReadSavedPositions(string file)
    {
        using var fileStream = File.OpenRead(file);
        var sections = IniFile.ParseFile(file, fileStream);
        var nodesSection = sections.FirstOrDefault(x => x.Name.Equals("nodes", StringComparison.OrdinalIgnoreCase));
        if (nodesSection is not null)
        {
            int count = 0;
            foreach (var nodePos in nodesSection)
            {
                if (nodePos.Count < 4)
                {
                    FLLog.Warning("MissionScriptEditor", "Invalid node position in nodes section!");
                    continue;
                }

                var type = nodePos[0].ToString()!;

                if (type.Equals("comment", StringComparison.OrdinalIgnoreCase))
                {
                    var name = nodePos[1].ToString()!;
                    var pos = new Vector2(nodePos[2].ToSingle(), nodePos[3].ToSingle());
                    var comment = new CommentNode()
                    {
                        BlockName = name
                    };
                    nodes.Add(comment);
                    nodeRelocationQueue.Enqueue((comment.Id, pos));
                    continue;
                }

                var triggerNickname = nodePos[1].ToString()!;
                var xPos = nodePos[2].ToSingle();
                var yPos = nodePos[3].ToSingle();

                var trigger = triggers.FirstOrDefault(x => x.Data.Nickname == triggerNickname);
                if (trigger is null)
                {
                    FLLog.Warning("MissionScriptEditor", $"Trigger from {type} node in the nodes section was not found.");
                    continue;
                }

                if (type.Equals("trigger", StringComparison.OrdinalIgnoreCase))
                {
                    nodeRelocationQueue.Enqueue((trigger.Id, new Vector2(xPos, yPos)));
                    count++;
                }
            }
            return count > 0;
        }
        else
        {
            return false;
        }
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
            while(columns.Count <= index)
                columns.Add(new List<NodeMissionTrigger>());
            foreach (var c in triggerNode.Actions) {
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

    public override void Draw(double elapsed)
    {
        //ImGuiHelper.AnimatingElement();
        if (!ImGui.BeginTable("ME Table", 3, ImGuiTableFlags.None))
        {
            return;
        }

        CheckIndexes();

        ImGui.TableSetupColumn("ME Left Sidebar", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("ME Node Editor", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("ME Right Sidebar", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        RenderLeftSidebar();

        ImGui.TableNextColumn();
        RenderNodeEditor();

        ImGui.TableNextColumn();
        RenderRightSidebar();

        ImGui.EndTable();

        popup.Run();
    }

    private void CheckIndexes()
    {
        if (selectedShipIndex is -1 && missionIni.Ships.Count is not 0)
        {
            selectedShipIndex = 0;
        }

        if (selectedArchIndex is -1 && missionIni.ShipIni.ShipArches.Count is not 0)
        {
            selectedArchIndex = 0;
        }

        if (selectedNpcIndex is -1 && missionIni.NPCs.Count is not 0)
        {
            selectedNpcIndex = 0;
        }

        if (selectedSolarIndex is -1 && missionIni.Solars.Count is not 0)
        {
            selectedSolarIndex = 0;
        }

        if (selectedFormationIndex is -1 && missionIni.Formations.Count is not 0)
        {
            selectedFormationIndex = 0;
        }

        if (selectedLootIndex is -1 && missionIni.Loots.Count is not 0)
        {
            selectedLootIndex = 0;
        }
    }

    private void RenderNodeEditor()
    {
        NodeEditor.SetCurrentEditor(context);
        NodeEditor.Begin("Node Editor", Vector2.Zero);

        var cursorTopLeft = ImGui.GetCursorScreenPos();

        while (nodeRelocationQueue.Count > 0)
        {
            var node = nodeRelocationQueue.Dequeue();
            NodeEditor.SetNodePosition(node.Id, node.Pos);
        }

        var lookups = new NodeLookups() { MissionIni = missionIni };
        foreach (var node in nodes)
        {
            var isFiltered = NodeFilter is not "" && node.InternalId.IndexOf(NodeFilter, StringComparison.OrdinalIgnoreCase) == -1;

            if (isFiltered)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.3f);
            }

            node.Render(gameData, popup, ref lookups);

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
    }

    private bool TryLinkNodes(Node start, Node end, LinkType linkType)
    {
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

        startPin.CreateLink(ref nextId, endPin, null);
        return true;
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
                var link = startPin.CreateLink(ref nextId, endPin, ShowLabel);
                if (link is not null)
                {
                    foreach (var node in nodes)
                    {
                        node.OnLinkCreated(link);
                    }
                }
            }
        }

        NodeEditor.EndCreate();

        if (NodeEditor.BeginDelete())
        {
            if (NodeEditor.QueryDeletedLink(out var linkId) && NodeEditor.AcceptDeletedItem())
            {
                NodePin.DeleteLink(linkId);
            }
        }

        NodeEditor.EndDelete();
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
            var node = new NodeMissionTrigger(null);
            nodes.Add(node);
            triggers.Add(node);
            nodeRelocationQueue.Enqueue((node.Id, position));
        }

        if (ImGui.MenuItem("Comment Node"))
        {
            var node = new CommentNode();
            nodes.Add(node);
            nodeRelocationQueue.Enqueue((node.Id, position));
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
        if (!node.OnContextMenu(popup))
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

            NodeEditor.DeleteNode(nodes[nodeIdx].Id);
            nodes.RemoveAt(nodeIdx);
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

    public override void Dispose()
    {
        context.Dispose();
        config.Dispose();
        base.Dispose();
    }

    internal EditResult<bool> SaveMission(string savePath = null)
    {
        if (savePath != null)
        {
            FileSaveLocation = savePath;
        }

        IniBuilder ini = new();

        ini.Section("Mission")
            .Entry("mission_title", missionIni.Info.MissionTitle)
            .Entry("mission_offer", missionIni.Info.MissionOffer)
            .Entry("reward", missionIni.Info.Reward)
            .Entry("npc_ship_file", missionIni.Info.NpcShipFile);

        foreach (var npc in missionIni.NPCs)
        {
            IniSerializer.SerializeMissionNpc(npc, ini);
        }

        foreach (var objective in missionIni.Objectives)
        {
            IniSerializer.SerializeMissionObjective(objective, ini);
        }

        foreach (var loot in missionIni.Loots)
        {
            IniSerializer.SerializeMissionLoot(loot, ini);
        }

        foreach (var dialog in missionIni.Dialogs)
        {
            IniSerializer.SerializeMissionDialog(dialog, ini);
        }

        foreach (var objectiveList in missionIni.ObjLists)
        {
            IniSerializer.SerializeMissionObjectiveList(objectiveList, ini);
        }

        foreach (var solar in missionIni.Solars)
        {
            IniSerializer.SerializeMissionSolar(solar, ini);
        }

        foreach (var ship in missionIni.Ships)
        {
            IniSerializer.SerializeMissionShip(ship, ini);
        }

        foreach (var formation in missionIni.Formations)
        {
            IniSerializer.SerializeMissionFormation(formation, ini);
        }

        // Save nodes
        foreach (var tr in nodes.OfType<NodeMissionTrigger>())
        {
            tr.WriteNode(this, ini);
        }

        // Store the locations of our nodes
        var s = ini.Section("Nodes");

        NodeEditor.SetCurrentEditor(context);
        foreach (var trigger in triggers)
        {
            var pos = NodeEditor.GetNodePosition(trigger.Id);
            s.Entry("node", "Trigger", trigger.Data.Nickname, pos.X, pos.Y);
        }
        foreach (var node in nodes.OfType<CommentNode>())
        {
            var pos = NodeEditor.GetNodePosition(node.Id);
            s.Entry("node", "Comment", node.BlockName, pos.X, pos.Y);
        }

        NodeEditor.SetCurrentEditor(null);
        IniWriter.WriteIniFile(FileSaveLocation, ini.Sections);

        return new EditResult<bool>(true);
    }
}
