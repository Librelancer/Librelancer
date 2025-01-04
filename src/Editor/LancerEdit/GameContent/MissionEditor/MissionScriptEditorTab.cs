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
using LibreLancer;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
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
    private readonly List<NodeLink> links;
    private int nextId;
    private NodePin newLinkPin;

    private readonly MissionIni missionIni;

    public MissionScriptEditorTab(GameDataContext gameData, MainWindow win, string file)
    {
        Title = $"Mission Script Editor - {Path.GetFileName(file)}";
        this.gameData = gameData;
        this.win = win;
        popup = new PopupManager();

        config = new NodeEditorConfig();
        context = new NodeEditorContext(config);

        NodeBuilder.LoadTexture(win.RenderContext);

        nodes = [];
        links = [];
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
                BlueprintNode node = action.Type switch
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

                var linked = TryLinkNodes(triggerNode, node, LinkType.Action);
                Debug.Assert(linked);

                if (node is NodeActActivateTrigger or NodeActDeactivateTrigger)
                {
                    actionsThatLinkToTriggers.Add(node);
                }

                nodes.Add(node);
            }

            foreach (var condition in trigger.Conditions)
            {
                BlueprintNode node = condition.Type switch
                {
                    TriggerConditions.Cnd_WatchVibe => new NodeCndWatchVibe(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_WatchTrigger => new NodeCndWatchTrigger(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_True => new NodeCndTrue(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_TLExited => new NodeCndTradeLaneExit(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_TLEntered => new NodeCndTradeLaneEnter(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_Timer => new NodeCndTimer(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_TetherBroke => new NodeCndTetherBreak(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_SystemExit => new NodeCndSystemExit(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_SystemEnter => new NodeCndSystemEnter(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_SpaceExit => new NodeCndSpaceExit(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_SpaceEnter => new NodeCndSpaceEnter(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_RumorHeard => new NodeCndRumourHeard(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_RTCDone => new NodeCndRtcComplete(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_ProjHitShipToLbl => new NodeCndProjectileHitShipToLabel(ref nextId,
                        condition.Entry),
                    TriggerConditions.Cnd_ProjHit => new NodeCndProjectileHit(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_PopUpDialog => new NodeCndPopUpDialog(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_PlayerManeuver => new NodeCndPlayerManeuver(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_PlayerLaunch => new NodeCndPlayerLaunch(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_NPCSystemExit => new NodeCndNpcSystemExit(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_NPCSystemEnter => new NodeCndNpcSystemEnter(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_MsnResponse => new NodeCndMissionResponse(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_LootAcquired => new NodeCndLootAcquired(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_LocExit => new NodeCndLocationExit(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_LocEnter => new NodeCndLocationEnter(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_LaunchComplete => new NodeCndLaunchComplete(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_JumpInComplete => new NodeCndJumpInComplete(ref nextId, condition.Entry),
                    //TriggerConditions.Cnd_JumpgateAct => // need examples of what this one looks like
                    TriggerConditions.Cnd_InZone => new NodeCndInZone(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_InTradelane => new NodeCndInTradeLane(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_InSpace => new NodeCndInSpace(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_HealthDec => new NodeCndHealthDecreased(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_HasMsn => new NodeCndHasMission(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_EncLaunched => new NodeCndEncounterLaunched(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_DistVecLbl => new NodeCndShipDistanceVectorLabel(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_DistVec => new NodeCndShipDistanceVector(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_DistShip => new NodeCndShipDistance(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_DistCircle => new NodeCndShipDistanceCircle(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_Destroyed => new NodeCndDestroyed(ref nextId, condition.Entry),
                    //TriggerConditions.Cnd_CmpToPlane => need examples of this one too
                    TriggerConditions.Cnd_CommComplete => new NodeCndCommComplete(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_CharSelect => new NodeCndCharacterSelect(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_CargoScanned => new NodeCndCargoScanned(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_BaseExit => new NodeCndBaseExit(ref nextId, condition.Entry),
                    TriggerConditions.Cnd_BaseEnter => new NodeCndBaseEnter(ref nextId, condition.Entry),
                    _ => throw new NotImplementedException($"{condition.Type} is not implemented")
                };

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
                _ => throw new InvalidCastException()
            };

            var trigger = triggers.FirstOrDefault(x => x.Data.Nickname == triggerTarget);
            if (trigger is null)
            {
                FLLog.Warning("MissionScriptEditor", "An activate trigger action had a trigger that was not valid!");
                continue;
            }

            TryLinkNodes(action, trigger, LinkType.Trigger);
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

    private bool firstRender;
    private void RenderNodeEditor()
    {
        NodeEditor.SetCurrentEditor(context);
        NodeEditor.Begin("Node Editor", Vector2.Zero);

        if (!firstRender)
        {
            firstRender = true;

            // If there is no positional data, we try to arrange them into a grid like structure to make it easier to use

            int sortIndex = 0;
            var startingXPos = 0f;
            var triggerYPos = 0f;
            var actionYPos = 0f;
            var conditionYPos = 0f;
            foreach (var trigger in triggers)
            {
                if (sortIndex > 10)
                {
                    sortIndex = 0;
                    startingXPos += 1600f;
                    triggerYPos = actionYPos = conditionYPos = 0f;
                }

                NodeEditor.SetNodePosition(trigger.Id, new Vector2(startingXPos, triggerYPos));
                foreach (var action in GetLinkedNodes(trigger, PinKind.Output, LinkType.Action))
                {
                    NodeEditor.SetNodePosition(action.Id, new Vector2(startingXPos + 600f, actionYPos));
                    actionYPos += 100f;
                }

                foreach (var condition in GetLinkedNodes(trigger, PinKind.Output, LinkType.Condition))
                {
                    NodeEditor.SetNodePosition(condition.Id, new Vector2(startingXPos + 1200f, conditionYPos));
                    conditionYPos += 100f;
                }

                triggerYPos = Math.Max(conditionYPos, actionYPos) + 100f;
                conditionYPos = actionYPos = triggerYPos;
                sortIndex++;
            }
        }

        foreach (var node in nodes)
        {
            node.Render(gameData, popup, missionIni);
        }

        foreach (var link in links)
        {
            NodeEditor.Link(link.Id, link.StartPin.Id, link.EndPin.Id, link.Color, 2.0f);
        }

        TryCreateLink();

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

        links.Add(new NodeLink(nextId++, startPin, endPin, end.Color));
        return true;
    }

    private void TryCreateLink()
    {
        if (NodeEditor.BeginCreate(Color4.White, 2.0f))
        {
            void ShowLabel(string label, Color4 color)
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetTextLineHeight());
                var size = ImGui.CalcTextSize(label);

                var padding = ImGui.GetStyle().FramePadding;
                var spacing = ImGui.GetStyle().ItemSpacing;

                ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(spacing.X, -spacing.Y));

                var rectMin = ImGui.GetCursorScreenPos() - padding;
                var rectMax = ImGui.GetCursorScreenPos() + size + padding;

                var drawList = ImGui.GetWindowDrawList();
                drawList.AddRectFilled(rectMin, rectMax, ImGui.ColorConvertFloat4ToU32(color), size.Y * 0.15f);
                ImGui.TextUnformatted(label);
            }

            if (NodeEditor.QueryNewLink(out var startPinId, out var endPinId))
            {
                var startPin = FindPin(startPinId);
                var endPin = FindPin(endPinId);

                newLinkPin = startPin ?? endPin;

                Debug.Assert(startPin != null, nameof(startPin) + " != null");

                if (startPin.PinKind == PinKind.Input)
                {
                    // Swap pins
                    (startPin, endPin) = (endPin, startPin);
                }

                // If we are dragging a pin and hovering a pin, check if we can connect
                if (startPin is not null && endPin is not null)
                {
                    if (endPin == startPin)
                    {
                        NodeEditor.RejectNewItem(Color4.Red, 2.0f);
                    }
                    else if (endPin.PinKind == startPin.PinKind)
                    {
                        ShowLabel("x Incompatible Pin Kind", new Color4(45, 32, 32, 180));
                        NodeEditor.RejectNewItem(Color4.Red, 2.0f);
                    }
                    else if (endPin.OwnerNode == startPin.OwnerNode)
                    {
                        ShowLabel("x Cannot connect to self", new Color4(45, 32, 32, 180));
                        NodeEditor.RejectNewItem(Color4.Red, 1.0f);
                    }
                    else if (endPin.LinkType != startPin.LinkType)
                    {
                        ShowLabel("x Incompatible Link Type", new Color4(45, 32, 32, 180));
                        NodeEditor.RejectNewItem(new Color4(255, 128, 128, 255));
                    }
                    else if (links.Any(x => x.StartPin == startPin && x.EndPin == endPin))
                    {
                        ShowLabel("x Link already exists", new Color4(45, 32, 32, 180));
                        NodeEditor.RejectNewItem(new Color4(255, 128, 128, 255));
                    }
                    else
                    {
                        ShowLabel("+ Create Link", new Color4(32, 45, 32, 180));
                        if (NodeEditor.AcceptNewItem(new Color4(128, 255, 128, 255), 4.0f))
                        {
                            var nodeLink = new NodeLink(nextId++, startPin, endPin)
                            {
                                Color = endPin.OwnerNode.Color
                            };
                            links.Add(nodeLink);
                        }
                    }
                }
            }

            if (NodeEditor.QueryNewNode(out var newPinId))
            {
                newLinkPin = FindPin(newPinId);
                if (newLinkPin is not null)
                {
                    ShowLabel("+ Create Node", new Color4(32, 45, 32, 180));
                }

                if (NodeEditor.AcceptNewItem())
                {
                    // TODO createNewNode = true;
                    newLinkPin = null;
                    NodeEditor.Suspend();
                    ImGui.OpenPopup("Create New Node");
                    NodeEditor.Resume();
                }
            }
        }
        else
        {
            newLinkPin = null;
        }

        NodeEditor.EndCreate();
    }

    private NodePin FindPin(PinId id)
    {
        if (id.Value.ToInt64() == 0)
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

    private List<Node> GetLinkedNodes([NotNull] Node node, PinKind kind, LinkType? pinFilter = null)
    {
        var nodes = new List<Node>();
        var inPins = kind == PinKind.Input;
        var pins = inPins ? node.Inputs : node.Outputs;
        foreach (var pin in pins.Where(x => pinFilter == null || x.LinkType == pinFilter))
        {
            nodes.AddRange(links
                .Where(x => inPins ? x.EndPin == pin : x.StartPin == pin)
                .Select(link => inPins ? link.StartPin.OwnerNode : link.EndPin.OwnerNode));
        }

        return nodes;
    }

    public override void Dispose()
    {
        context.Dispose();
        config.Dispose();
        base.Dispose();
    }
}
