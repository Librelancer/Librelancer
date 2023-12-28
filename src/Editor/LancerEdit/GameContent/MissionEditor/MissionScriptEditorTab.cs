using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

using Reg = LancerEdit.GameContent.MissionEditor.Registers.Registers;

namespace LancerEdit.GameContent.MissionEditor;
public sealed class MissionScriptEditorTab : GameContentTab
{
    private GameDataContext gameData;
    private MainWindow win;

    private readonly NodeEditorConfig config;
    private readonly NodeEditorContext context;

    private List<Node> nodes;
    private List<NodeLink> links;

    private int nodeId = 0;
    private int pinId = 0;

    private NodePin newLinkPin = null;
    private NodePin newNodeLinkPin = null;

    private static bool registeredNodeValueRenderers = false;

    private MissionScript missionScript;

    public MissionScriptEditorTab(GameDataContext gameData, MainWindow win, string file)
    {
        Title = "Mission Script Editor";
        this.gameData = gameData;
        this.win = win;
        config = new NodeEditorConfig();
        context = new NodeEditorContext(config);
        NodeBuilder.LoadTexture();

        RegisterNodeValues();

        nodes = new List<Node>();
        links = new List<NodeLink>();
        missionScript = new MissionScript(new MissionIni(file, null));

        foreach (var ship in missionScript.Ships)
        {
            nodes.Add(new BlueprintNode<MissionShip>(ref nodeId, "Mission Ship", ship.Value, Color4.FromRgba((uint)NodeColours.MissionShip)));
        }

        foreach (var solar in missionScript.Solars)
        {
            nodes.Add(new BlueprintNode<MissionSolar>(ref nodeId, "Mission Solar", solar.Value, Color4.FromRgba((uint)NodeColours.MissionSolar)));
        }
    }

    private int selectedItem = 0;
    private VectorIcon selectedIcon;
    public override void Draw(double elapsed)
    {
        ImGuiHelper.AnimatingElement();
        NodeEditor.SetCurrentEditor(context);
        NodeEditor.Begin("Node Editor", Vector2.Zero);

        foreach (var node in nodes)
        {
            node.Render(gameData, missionScript);
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
                            var nodeLink = new NodeLink(pinId++, startPin, endPin)
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
                    newNodeLinkPin = FindPin(pinId);
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
        if (registeredNodeValueRenderers)
        {
            return;
        }

        registeredNodeValueRenderers = true;

        Node.RegisterNodeValueRenderer<MissionShip>(Reg.MissionShipContent);
        Node.RegisterNodeValueRenderer<MissionSolar>(Reg.MissionSolarContent);
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
