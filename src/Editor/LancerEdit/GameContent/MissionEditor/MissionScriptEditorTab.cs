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
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using Microsoft.EntityFrameworkCore.Query;
using SimpleMesh.Formats.Collada.Schema;
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

        var actionsThatLinkToTriggers = new List<BlueprintNode>();
        foreach (var trigger in missionIni.Triggers)
        {
            var triggerNode = new NodeMissionTrigger(ref nextId, trigger);
            foreach (var action in trigger.Actions)
            {
                var node = ActionToNode(action.Type, action);
                var linked = TryLinkNodes(triggerNode, node, LinkType.Action);
                Debug.Assert(linked);

                if (node is NodeActActivateTrigger or NodeActDeactivateTrigger or NodeActSave)
                {
                    actionsThatLinkToTriggers.Add(node);
                }

                nodes.Add(node);
            }

            foreach (var condition in trigger.Conditions)
            {
                var node = ConditionToNode(condition.Type, condition.Entry);

                var linked = TryLinkNodes(triggerNode, node, LinkType.Condition);
                Debug.Assert(linked);

                nodes.Add(node);
            }

            triggers.Add(triggerNode);
            nodes.Add(triggerNode);
        }

        foreach (var action in actionsThatLinkToTriggers)
        {
            var triggerTarget = action switch
            {
                NodeActActivateTrigger act => act.Data.Trigger,
                NodeActDeactivateTrigger deactivate => deactivate.Data.Trigger,
                NodeActSave save => save.Data.Trigger,
                _ => throw new InvalidCastException()
            };

            var trigger = triggers.FirstOrDefault(x => x.Data.Nickname == triggerTarget);
            if (trigger is null)
            {
                FLLog.Warning("MissionScriptEditor", $"An activate trigger action had a trigger that was not valid! ({triggerTarget})");
                continue;
            }

            TryLinkNodes(action, trigger, LinkType.Trigger);
        }

        using var fileStream = File.OpenRead(file);
        var sections = IniFile.ParseFile(file, fileStream);
        var nodesSection = sections.FirstOrDefault(x => x.Name.Equals("nodes", StringComparison.OrdinalIgnoreCase));
        if (nodesSection is not null)
        {
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
                var index = nodePos.Count >= 5 ? nodePos[4].ToInt32() : -1;

                var trigger = triggers.FirstOrDefault(x => x.Data.Nickname == triggerNickname);
                if (trigger is null)
                {
                    FLLog.Warning("MissionScriptEditor", $"Trigger from {type} node in the nodes section was not found.");
                    continue;
                }

                if (type.Equals("trigger", StringComparison.OrdinalIgnoreCase))
                {
                    nodeRelocationQueue.Enqueue((trigger.Id, new Vector2(xPos, yPos)));
                }
                else if (type.Equals("action", StringComparison.OrdinalIgnoreCase))
                {
                    var actions = GetLinkedNodes(trigger, PinKind.Output, LinkType.Action);
                    if (actions.Count <= index || index == -1)
                    {
                        FLLog.Warning("MissionScriptEditor", $"Action in the nodes section had a bad index.");
                        continue;
                    }

                    nodeRelocationQueue.Enqueue((actions.ElementAt(index).Id, new Vector2(xPos, yPos)));
                }
                else if (type.Equals("condition", StringComparison.OrdinalIgnoreCase))
                {
                    var conditions = GetLinkedNodes(trigger, PinKind.Output, LinkType.Condition);
                    if (conditions.Count <= index || index == -1)
                    {
                        FLLog.Warning("MissionScriptEditor", $"Condition in the nodes section had a bad index.");
                        continue;
                    }

                    nodeRelocationQueue.Enqueue((conditions.ElementAt(index).Id, new Vector2(xPos, yPos)));
                }
            }

            return;
        }

        // Arrange initial positions for all nodes if needed
        List<NodeMissionTrigger> processedTriggers = [];
        Dictionary<int, float> columnHeights = [];
        Dictionary<NodeMissionTrigger, float> triggerColumnMinMaxHeight = [];

        var firstTrigger = triggers.FirstOrDefault(x => x.Data.InitState == TriggerInitState.ACTIVE);
        if (firstTrigger != null)
        {
            void CalculateNodeTreeSize(NodeMissionTrigger trigger, int column)
            {
                if (processedTriggers.Contains(trigger))
                {
                    return;
                }

                Dictionary<string, NodeMissionTrigger> triggersDictionary = triggers.ToDictionary(x => x.Data.Nickname, x => x);
                var actions =  GetLinkedNodes(trigger, PinKind.Output, LinkType.Action);
                var conditions =  GetLinkedNodes(trigger, PinKind.Output, LinkType.Condition);
                var triggerActions = actions.OfType<NodeActActivateTrigger>().Select(x => x.Data.Trigger).Concat(actions.OfType<NodeActSave>().Select(x => x.Data.Trigger));
                var nextTriggers = triggersDictionary.Where(x => triggerActions.Any(y => y == x.Key)).ToDictionary().Values;

                float maxY = 200 * actions.Count + 200 * conditions.Count;
                if (!columnHeights.TryAdd(column + 1, maxY))
                {
                    var height = columnHeights[column + 1] + maxY;
                    columnHeights[column + 1] = height;
                }

                triggerColumnMinMaxHeight[trigger] = 100f * (actions.Count + conditions.Count);
                processedTriggers.Add(trigger);

                foreach (var nextTrigger in nextTriggers)
                {
                    CalculateNodeTreeSize(nextTrigger, column + 2);
                }
            }

            void ProcessTrigger(NodeMissionTrigger trigger, int column)
            {
                if (processedTriggers.Contains(trigger))
                {
                    return;
                }

                Dictionary<string, NodeMissionTrigger> triggersDictionary = triggers.ToDictionary(x => x.Data.Nickname, x => x);
                var actions =  GetLinkedNodes(trigger, PinKind.Output, LinkType.Action);
                var conditions =  GetLinkedNodes(trigger, PinKind.Output, LinkType.Condition);
                var triggerActions = actions.OfType<NodeActActivateTrigger>().Select(x => x.Data.Trigger);
                var nextTriggers = triggersDictionary.Where(x => triggerActions.Any(y => y == x.Key)).ToDictionary().Values;

                processedTriggers.Add(trigger);

                var nextYPos = columnHeights[column + 1];
                foreach (var action in actions)
                {
                    var height = columnHeights[column + 1];
                    columnHeights[column + 1] -= 200;
                    nodeRelocationQueue.Enqueue((action.Id, new Vector2((column + 1) * 500f, height)));
                }

                foreach (var condition in conditions)
                {
                    var height = columnHeights[column + 1];
                    columnHeights[column + 1] -= 200;
                    nodeRelocationQueue.Enqueue((condition.Id, new Vector2((column + 1) * 500f, height)));
                }

                foreach (var nextTrigger in nextTriggers)
                {
                    nextYPos -= triggerColumnMinMaxHeight[trigger];
                    nodeRelocationQueue.Enqueue((nextTrigger.Id, new Vector2((column + 2) * 500f, nextYPos)));
                    ProcessTrigger(nextTrigger, column + 2);
                }
            }

            CalculateNodeTreeSize(firstTrigger, 0);
            processedTriggers.Clear();

            ProcessTrigger(firstTrigger, 0);
            nodeRelocationQueue.Enqueue((firstTrigger.Id, Vector2.Zero));
        }
    }

    public override void Draw(double elapsed)
    {
        ImGuiHelper.AnimatingElement();
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
            node.Render(gameData, popup, ref lookups);
        }

        foreach (var link in NodePin.AllLinks)
        {
            NodeEditor.Link(link.Id, link.StartPin.Id, link.EndPin.Id, link.Color, 2.0f);
        }

        //if (!createNewNode)
        {
            TryCreateLink();
            ImGui.SetCursorScreenPos(cursorTopLeft);
        }

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
            ImGui.TextUnformatted(label);
        }
    }

    private void CreateNewNodeContextMenu(Vector2 position)
    {
        ImGui.Text("Create New Node");
        ImGui.Separator();

        if (ImGui.MenuItem("Trigger"))
        {
            var node = new NodeMissionTrigger(ref nextId, null);
            nodes.Add(node);
            triggers.Add(node);
            nodeRelocationQueue.Enqueue((node.Id, position));
        }

        if (ImGui.MenuItem("Action"))
        {
            popup.OpenPopup(new NewActionPopup(action =>
            {
                var node = ActionToNode(action, null);
                nodes.Add(node);
                nodeRelocationQueue.Enqueue((node.Id, position));
            }));
        }

        if (ImGui.MenuItem("Condition"))
        {
            popup.OpenPopup(new NewConditionPopup(condition =>
            {
                var node = ConditionToNode(condition, null);
                nodes.Add(node);
                nodeRelocationQueue.Enqueue((node.Id, position));
            }));
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

        ImGui.TextUnformatted("Node Context Menu");
        if (selectedNodes is 1)
        {
            ImGui.Separator();
            ImGui.Text($"ID: {node.Id}");
            ImGui.Text($"Type: {node.GetType().Name}");
            ImGui.Text($"Inputs: {node.Inputs.Count}");
            ImGui.Text($"Outputs: {node.Outputs.Count}");
        }

        ImGui.Separator();
        if (!ImGui.MenuItem("Delete"))
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

    private NodePin FindPin(PinId id)
    {
        if (id == 0)
        {
            return null;
        }

        foreach (var node in nodes)
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
        }

        return null;
    }

    internal List<Node> GetLinkedNodes([NotNull] Node node, PinKind kind, LinkType? pinFilter = null)
    {
        var linkedNodes = new List<Node>();
        var inPins = kind == PinKind.Input;
        var pins = inPins ? node.Inputs : node.Outputs;
        foreach (var pin in pins.Where(x => pinFilter == null || x.LinkType == pinFilter))
        {
            linkedNodes.AddRange(NodePin.AllLinks
                .Where(x => inPins ? x.EndPin == pin : x.StartPin == pin)
                .Select(link => inPins ? link.StartPin.OwnerNode : link.EndPin.OwnerNode));
        }

        return linkedNodes;
    }

    private BlueprintNode ActionToNode(TriggerActions type, MissionAction action)
    {
        return type switch
        {
            TriggerActions.Act_PlaySoundEffect => new NodeActPlaySound(ref nextId, action),
            TriggerActions.Act_Invulnerable => new NodeActInvulnerable(ref nextId, action),
            TriggerActions.Act_PlayMusic => new NodeActPlayMusic(ref nextId, action),
            TriggerActions.Act_SetShipAndLoadout => new NodeActSetShipAndLoadout(ref nextId, action),
            TriggerActions.Act_RemoveAmbient => new NodeActRemoveAmbient(ref nextId, action),
            TriggerActions.Act_AddAmbient => new NodeActAddAmbient(ref nextId, action),
            TriggerActions.Act_RemoveRTC => new NodeActRemoveRtc(ref nextId, action),
            TriggerActions.Act_AddRTC => new NodeActAddRtc(ref nextId, action),
            TriggerActions.Act_AdjAcct => new NodeActAdjustAccount(ref nextId, action),
            TriggerActions.Act_DeactTrig => new NodeActDeactivateTrigger(ref nextId, action),
            TriggerActions.Act_ActTrig => new NodeActActivateTrigger(ref nextId, action),
            TriggerActions.Act_SetNNObj => new NodeActSetNNObject(ref nextId, action),
            TriggerActions.Act_ForceLand => new NodeActForceLand(ref nextId, action),
            TriggerActions.Act_LightFuse => new NodeActLightFuse(ref nextId, action),
            TriggerActions.Act_PopUpDialog => new NodeActPopupDialog(ref nextId, action),
            TriggerActions.Act_ChangeState => new NodeActChangeState(ref nextId, action),
            TriggerActions.Act_RevertCam => new NodeActRevertCamera(ref nextId, action),
            TriggerActions.Act_CallThorn => new NodeActCallThorn(ref nextId, action),
            TriggerActions.Act_MovePlayer => new NodeActMovePlayer(ref nextId, action),
            TriggerActions.Act_Cloak => new NodeActCloak(ref nextId, action),
            TriggerActions.Act_PobjIdle => new NodeActPObjectIdle(ref nextId, action),
            TriggerActions.Act_SetInitialPlayerPos => new NodeActSetInitialPlayerPos(ref nextId, action),
            TriggerActions.Act_RelocateShip => new NodeActRelocateShip(ref nextId, action),
            TriggerActions.Act_StartDialog => new NodeActStartDialog(ref nextId, action),
            TriggerActions.Act_SendComm => new NodeActSendComm(ref nextId, action),
            TriggerActions.Act_EtherComm => new NodeActEtherComm(ref nextId, action),
            TriggerActions.Act_SetVibe => new NodeActSetVibe(ref nextId, action),
            TriggerActions.Act_SetVibeLbl => new NodeActSetVibeLabel(ref nextId, action),
            TriggerActions.Act_SetVibeShipToLbl => new NodeActSetVibeShipToLabel(ref nextId, action),
            TriggerActions.Act_SetVibeLblToShip => new NodeActSetVibeLabelToShip(ref nextId, action),
            TriggerActions.Act_SpawnSolar => new NodeActSpawnSolar(ref nextId, action),
            TriggerActions.Act_SpawnShip => new NodeActSpawnShip(ref nextId, action),
            TriggerActions.Act_SpawnFormation => new NodeActSpawnFormation(ref nextId, action),
            TriggerActions.Act_MarkObj => new NodeActMarkObject(ref nextId, action),
            TriggerActions.Act_Destroy => new NodeActDestroy(ref nextId, action),
            TriggerActions.Act_StaticCam => new NodeActStaticCamera(ref nextId, action),
            TriggerActions.Act_SpawnLoot => new NodeActSpawnLoot(ref nextId, action),
            TriggerActions.Act_SetVibeOfferBaseHack => new NodeActSetVibeOfferBaseHack(ref nextId, action),
            TriggerActions.Act_SetTitle => new NodeActSetTitle(ref nextId, action),
            TriggerActions.Act_SetRep => new NodeActSetRep(ref nextId, action),
            TriggerActions.Act_SetOrient => new NodeActSetOrientation(ref nextId, action),
            TriggerActions.Act_SetOffer => new NodeActSetOffer(ref nextId, action),
            TriggerActions.Act_SetNNState => new NodeActSetNNState(ref nextId, action),
            TriggerActions.Act_SetNNHidden => new NodeActSetNNHidden(ref nextId, action),
            TriggerActions.Act_SetLifeTime => new NodeActSetLifetime(ref nextId, action),
            TriggerActions.Act_Save => new NodeActSave(ref nextId, action),
            TriggerActions.Act_RpopTLAttacksEnabled => new NodeActRPopAttacksEnabled(ref nextId, action),
            TriggerActions.Act_RpopAttClamp => new NodeActRPopClamp(ref nextId, action),
            TriggerActions.Act_RemoveCargo => new NodeActRemoveCargo(ref nextId, action),
            TriggerActions.Act_RandomPopSphere => new NodeActRandomPopSphere(ref nextId, action),
            TriggerActions.Act_RandomPop => new NodeActRandomPop(ref nextId, action),
            TriggerActions.Act_SetPriority => new NodeActSetPriority(ref nextId, action),
            TriggerActions.Act_PlayerEnemyClamp => new NodeActPlayerEnemyClamp(ref nextId, action),
            TriggerActions.Act_PlayerCanTradelane => new NodeActCanTradeLane(ref nextId, action),
            TriggerActions.Act_PlayerCanDock => new NodeActCanDock(ref nextId, action),
            TriggerActions.Act_NNIds => new NodeActNNIds(ref nextId, action),
            TriggerActions.Act_NNPath => new NodeActNNPath(ref nextId, action),
            TriggerActions.Act_NagOff => new NodeActNagOff(ref nextId, action),
            TriggerActions.Act_NagGreet => new NodeActNagGreet(ref nextId, action),
            TriggerActions.Act_NagDistTowards => new NodeActNagDistTowards(ref nextId, action),
            TriggerActions.Act_NagDistLeaving => new NodeActNagDistLeaving(ref nextId, action),
            TriggerActions.Act_NagClamp => new NodeActNagClamp(ref nextId, action),
            TriggerActions.Act_LockManeuvers => new NodeActLockManeuvers(ref nextId, action),
            TriggerActions.Act_LockDock => new NodeActLockDock(ref nextId, action),
            TriggerActions.Act_Jumper => new NodeActJumper(ref nextId, action),
            TriggerActions.Act_HostileClamp => new NodeActHostileClamp(ref nextId, action),
            TriggerActions.Act_GiveObjList => new NodeActGiveObjectList(ref nextId, action),
            TriggerActions.Act_GiveNNObjs => new NodeActGiveNNObjectives(ref nextId, action),
            TriggerActions.Act_GCSClamp => new NodeActGcsClamp(ref nextId, action),
            TriggerActions.Act_EnableManeuver => new NodeActEnableManeuver(ref nextId, action),
            TriggerActions.Act_EnableEnc => new NodeActEnableEncounter(ref nextId, action),
            TriggerActions.Act_DockRequest => new NodeActDockRequest(ref nextId, action),
            TriggerActions.Act_DisableTradelane => new NodeActDisableTradelane(ref nextId, action),
            TriggerActions.Act_DisableFriendlyFire => new NodeActDisableFriendlyFire(ref nextId, action),
            TriggerActions.Act_DisableEnc => new NodeActDisableEncounter(ref nextId, action),
            TriggerActions.Act_AdjHealth => new NodeActAdjustHealth(ref nextId, action),
            _ => throw new NotImplementedException($"Unable to render node for action type: {action.Type}"),
        };
    }

    private BlueprintNode ConditionToNode(TriggerConditions condition, Entry entry)
    {
        return condition switch
                {
                    TriggerConditions.Cnd_WatchVibe => new NodeCndWatchVibe(ref nextId, entry),
                    TriggerConditions.Cnd_WatchTrigger => new NodeCndWatchTrigger(ref nextId, entry),
                    TriggerConditions.Cnd_True => new NodeCndTrue(ref nextId, entry),
                    TriggerConditions.Cnd_TLExited => new NodeCndTradeLaneExit(ref nextId, entry),
                    TriggerConditions.Cnd_TLEntered => new NodeCndTradeLaneEnter(ref nextId, entry),
                    TriggerConditions.Cnd_Timer => new NodeCndTimer(ref nextId, entry),
                    TriggerConditions.Cnd_TetherBroke => new NodeCndTetherBreak(ref nextId, entry),
                    TriggerConditions.Cnd_SystemExit => new NodeCndSystemExit(ref nextId, entry),
                    TriggerConditions.Cnd_SystemEnter => new NodeCndSystemEnter(ref nextId, entry),
                    TriggerConditions.Cnd_SpaceExit => new NodeCndSpaceExit(ref nextId, entry),
                    TriggerConditions.Cnd_SpaceEnter => new NodeCndSpaceEnter(ref nextId, entry),
                    TriggerConditions.Cnd_RumorHeard => new NodeCndRumourHeard(ref nextId, entry),
                    TriggerConditions.Cnd_RTCDone => new NodeCndRtcComplete(ref nextId, entry),
                    TriggerConditions.Cnd_ProjHitShipToLbl => new NodeCndProjectileHitShipToLabel(ref nextId, entry),
                    TriggerConditions.Cnd_ProjHit => new NodeCndProjectileHit(ref nextId, entry),
                    TriggerConditions.Cnd_PopUpDialog => new NodeCndPopUpDialog(ref nextId, entry),
                    TriggerConditions.Cnd_PlayerManeuver => new NodeCndPlayerManeuver(ref nextId, entry),
                    TriggerConditions.Cnd_PlayerLaunch => new NodeCndPlayerLaunch(ref nextId, entry),
                    TriggerConditions.Cnd_NPCSystemExit => new NodeCndNpcSystemExit(ref nextId, entry),
                    TriggerConditions.Cnd_NPCSystemEnter => new NodeCndNpcSystemEnter(ref nextId, entry),
                    TriggerConditions.Cnd_MsnResponse => new NodeCndMissionResponse(ref nextId, entry),
                    TriggerConditions.Cnd_LootAcquired => new NodeCndLootAcquired(ref nextId, entry),
                    TriggerConditions.Cnd_LocExit => new NodeCndLocationExit(ref nextId, entry),
                    TriggerConditions.Cnd_LocEnter => new NodeCndLocationEnter(ref nextId, entry),
                    TriggerConditions.Cnd_LaunchComplete => new NodeCndLaunchComplete(ref nextId, entry),
                    TriggerConditions.Cnd_JumpInComplete => new NodeCndJumpInComplete(ref nextId, entry),
                    //TriggerConditions.Cnd_JumpgateAct => // need examples of what this one looks like
                    TriggerConditions.Cnd_InZone => new NodeCndInZone(ref nextId, entry),
                    TriggerConditions.Cnd_InTradelane => new NodeCndInTradeLane(ref nextId, entry),
                    TriggerConditions.Cnd_InSpace => new NodeCndInSpace(ref nextId, entry),
                    TriggerConditions.Cnd_HealthDec => new NodeCndHealthDecreased(ref nextId, entry),
                    TriggerConditions.Cnd_HasMsn => new NodeCndHasMission(ref nextId, entry),
                    TriggerConditions.Cnd_EncLaunched => new NodeCndEncounterLaunched(ref nextId, entry),
                    TriggerConditions.Cnd_DistVecLbl => new NodeCndShipDistanceVectorLabel(ref nextId, entry),
                    TriggerConditions.Cnd_DistVec => new NodeCndShipDistanceVector(ref nextId, entry),
                    TriggerConditions.Cnd_DistShip => new NodeCndShipDistance(ref nextId, entry),
                    TriggerConditions.Cnd_DistCircle => new NodeCndShipDistanceCircle(ref nextId, entry),
                    TriggerConditions.Cnd_Destroyed => new NodeCndDestroyed(ref nextId, entry),
                    //TriggerConditions.Cnd_CmpToPlane => need examples of this one too
                    TriggerConditions.Cnd_CommComplete => new NodeCndCommComplete(ref nextId, entry),
                    TriggerConditions.Cnd_CharSelect => new NodeCndCharacterSelect(ref nextId, entry),
                    TriggerConditions.Cnd_CargoScanned => new NodeCndCargoScanned(ref nextId, entry),
                    TriggerConditions.Cnd_BaseExit => new NodeCndBaseExit(ref nextId, entry),
                    TriggerConditions.Cnd_BaseEnter => new NodeCndBaseEnter(ref nextId, entry),
                    _ => throw new NotImplementedException($"{condition} is not implemented")
                };
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

        Dictionary<NodeMissionTrigger, (TriggerEntryNode[] Actions, TriggerEntryNode[] Conditions)> triggerToActionAndConditions = new();
        foreach (var trigger in triggers)
        {
            trigger.WriteNode(this, ini);

            var actions =  GetLinkedNodes(trigger, PinKind.Output, LinkType.Action).OfType<TriggerEntryNode>().ToArray();
            var conditions =  GetLinkedNodes(trigger, PinKind.Output, LinkType.Condition).OfType<TriggerEntryNode>().ToArray();

            triggerToActionAndConditions.Add(trigger, (actions, conditions));
        }

        // Store the locations of our nodes
        var s = ini.Section("Nodes");

        NodeEditor.SetCurrentEditor(context);
        foreach (var pair in triggerToActionAndConditions)
        {
            var pos = NodeEditor.GetNodePosition(pair.Key.Id);
            s.Entry("node", "Trigger", pair.Key.Data.Nickname, pos.X, pos.Y);

            var i = 0;
            foreach (var action in pair.Value.Actions)
            {
                pos = NodeEditor.GetNodePosition(action.Id);
                s.Entry("node", "Action", pair.Key.Data.Nickname, pos.X, pos.Y, i++);
            }

            i = 0;
            foreach (var condition in pair.Value.Conditions)
            {
                pos = NodeEditor.GetNodePosition(condition.Id);
                s.Entry("node", "Condition", pair.Key.Data.Nickname, pos.X, pos.Y, i++);
            }
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
