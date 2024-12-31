using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Data.Missions;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using ImGui = ImGuiNET.ImGui;
using Reg = LancerEdit.GameContent.MissionEditor.Registers.Registers;

namespace LancerEdit.GameContent.MissionEditor;
public sealed partial class MissionScriptEditorTab : GameContentTab
{
    private readonly GameDataContext gameData;
    private MainWindow win;
    private PopupManager popup;

    private readonly NodeEditorConfig config;
    private readonly NodeEditorContext context;

    private readonly List<Node> nodes;
    private readonly List<NodeLink> links;

    private int nextId;

    private NodePin newLinkPin = null;
    private NodePin newNodeLinkPin = null;

    private static bool _registeredNodeValueRenderers = false;

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

        RegisterNodeValues();

        nodes = [];
        links = [];
        missionIni = new MissionIni(file, null);

        var npcPath = gameData.GameData.VFS.GetBackingFileName(gameData.GameData.DataPath(missionIni.Info.NpcShipFile));
        if (npcPath is not null)
        {
            missionIni.ShipIni = new NPCShipIni(npcPath, null);
        }

        foreach (var trigger in missionIni.Triggers)
        {
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
                    _ => null,
                };

                if (node is null)
                {
                    FLLog.Warning("MissionScriptEditor",
                        $"Unable to render node for action type: {action.GetType().FullName}");
                    continue;
                }

                nodes.Add(node);
            }

            nodes.Add(new NodeMissionTrigger(ref nextId, trigger));
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
                    else
                    {
                        ShowLabel("+ Create Link", new Color4(32, 45, 32, 180));
                        if (NodeEditor.AcceptNewItem(new Color4(128, 255, 128, 255), 4.0f))
                        {
                            var nodeLink = new NodeLink(nextId++, startPin, endPin)
                            {
                                Color = startPin.OwnerNode.Color
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

    private void RegisterNodeValues()
    {
        if (_registeredNodeValueRenderers)
        {
            return;
        }

        _registeredNodeValueRenderers = true;

        Node.RegisterNodeValueRenderer<MissionTrigger>(Reg.MissionTriggerContent);
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

    public override void Dispose()
    {
        context.Dispose();
        config.Dispose();
        base.Dispose();
    }
}
