using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes;

public class TreeNode : Node
{
    public TreeNode(int id, string name) : base(id, name, null, null)
    {
    }

    public override void Render(GameDataContext gameData, MissionScript missionScript)
    {
        const float Rounding = 5.0f;
        const float Padding = 12.0f;

        //Fetching nodebg from style not working in C#
        var pinBackground = new Color4(32, 32, 32, 200);

        NodeEditor.PushStyleColor(StyleColor.NodeBg, new Color4(128, 128, 128, 200));
        NodeEditor.PushStyleColor(StyleColor.NodeBorder, new Color4(32, 32, 32, 200));
        NodeEditor.PushStyleColor(StyleColor.PinRect, new Color4(60, 180, 255, 150));
        NodeEditor.PushStyleColor(StyleColor.PinRectBorder, new Color4(60, 180, 255, 150));

        NodeEditor.PushStyleVar(StyleVar.NodePadding, Vector4.Zero);
        NodeEditor.PushStyleVar(StyleVar.NodeRounding, Rounding);
        NodeEditor.PushStyleVar(StyleVar.SourceDirection, new Vector2(0, 1));
        NodeEditor.PushStyleVar(StyleVar.TargetDirection, new Vector2(0, -1));
        NodeEditor.PushStyleVar(StyleVar.LinkStrength, 0);
        NodeEditor.PushStyleVar(StyleVar.PinBorderWidth, 1);
        NodeEditor.PushStyleVar(StyleVar.PinRadius, 5);

        NodeEditor.BeginNode(Id);
        //Insert top padding and calculate size of top input rect
        ImGui.Dummy(new Vector2(Padding, Padding));
        var inputY = ImGui.GetItemRectMax().Y;
        ImGui.SameLine();
        ImGui.Dummy(new Vector2(1,1));
        var inputMin = ImGui.GetItemRectMin();
        Vector2 inputMax = new Vector2(0, inputY);
        //Content
        var popups = NodePopups.Begin(Id);

        ImGui.Dummy(new Vector2(Padding, 0));
        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.TextUnformatted(Name);
        if (Data != null)
        {
            if (NodeValueRenders.TryGetValue(Data.GetType(), out var renderer))
            {
                renderer(gameData, missionScript, ref popups, Data);
            }
        }
        ImGui.EndGroup();
        var topSz = ImGui.GetItemRectSize();
        inputMax.X = inputMin.X + topSz.X;
        ImGui.SameLine();
        ImGui.Dummy(new Vector2(Padding, 0));
        //Draw top input rect
        int inputAlpha = 200;
        if (Inputs.Count > 0)
        {
            NodeEditor.PushStyleVar(StyleVar.PinArrowSize, 10);
            NodeEditor.PushStyleVar(StyleVar.PinArrowWidth, 10);
            //yes it is actually meant to be a float :)
            NodeEditor.PushStyleVar(StyleVar.PinCorners, (float)ImDrawFlags.RoundCornersBottom);
            NodeEditor.BeginPin(Inputs[0].Id, PinKind.Input);
            NodeEditor.PinPivotRect(inputMin, inputMax);
            NodeEditor.PinRect(inputMin, inputMax);
            NodeEditor.EndPin();
            NodeEditor.PopStyleVar(3);
        }
        //Draw bottom output rect
        ImGui.Dummy(new Vector2(Padding, Padding));
        Vector2 outputMin = Vector2.Zero;
        Vector2 outputMax = Vector2.Zero;
        int outputAlpha = 200;
        if (Outputs.Count > 0)
        {
            ImGui.SameLine();
            outputMin = ImGui.GetCursorPos();
            outputMax = outputMin + new Vector2(topSz.X, Padding);
            NodeEditor.PushStyleVar(StyleVar.PinCorners, (float)ImDrawFlags.RoundCornersTop);
            NodeEditor.BeginPin(Outputs[0].Id, PinKind.Output);
            NodeEditor.PinPivotRect(outputMin, outputMax);
            NodeEditor.PinRect(outputMin, outputMax);
            NodeEditor.EndPin();
            NodeEditor.PopStyleVar();
        }

        NodeEditor.EndNode();
        NodeEditor.PopStyleVar(7);
        NodeEditor.PopStyleColor(4);
        popups.End();
        //Draw pin background
        var drawList = NodeEditor.GetNodeBackgroundDrawList(Id);
        if (Inputs.Count > 0)
        {
            drawList.AddRectFilled(
                inputMin + new Vector2(0,1),
                inputMax,
                ImGui.GetColorU32(pinBackground.ChangeAlpha(inputAlpha / 255f)),
                Rounding,
                ImDrawFlags.RoundCornersBottom
            );
            drawList.AddRect(
                inputMin + new Vector2(0,1),
                inputMax,
                ImGui.GetColorU32(pinBackground.ChangeAlpha(inputAlpha / 255f)),
                Rounding,
                ImDrawFlags.RoundCornersBottom
            );
        }

        if (Outputs.Count > 0)
        {
            drawList.AddRectFilled(
                outputMin,
                outputMax - new Vector2(0,1),
                ImGui.GetColorU32(pinBackground.ChangeAlpha(outputAlpha / 255f)),
                Rounding,
                ImDrawFlags.RoundCornersTop
            );
            drawList.AddRect(
                outputMin,
                outputMax - new Vector2(0,1),
                ImGui.GetColorU32(pinBackground.ChangeAlpha(outputAlpha / 255f)),
                Rounding,
                ImDrawFlags.RoundCornersTop
            );
        }
    }
}
