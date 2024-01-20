using System;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes;

public class BlueprintNode<T> : Node where T : class
{
    public BlueprintNode(ref int id, string name, object data, Color4? color = null) : base(id++, name, data, color)
    {
        Registers.Registers.RegisterNodeIo<T>(this, ref id);
    }

    public override void Render(GameDataContext gameData, MissionScript missionScript)
    {
        var iconSize  = new Vector2(24 * ImGuiHelper.Scale);
        var nb = NodeBuilder.Begin(Id);

        nb.Header(new Color4(Color.R, Color.G, Color.B, 255f));
        ImGui.Text(Name);
        nb.EndHeader();

        LayoutNode(Inputs.Select(x => x.Name), Outputs.Select(x => x.Name), 200);
        StartInputs();

        foreach (var pin in Inputs)
        {
            NodeEditor.BeginPin(pin.Id, PinKind.Input);
            VectorIcons.Icon(iconSize, VectorIcon.Flow, false);
            ImGui.SameLine();
            ImGui.Text(pin.Name);
            NodeEditor.EndPin();
        }

        StartOutputs();
        foreach (var pin in Outputs)
        {
            NodeEditor.BeginPin(pin.Id, PinKind.Output);
            VectorIcons.Icon(iconSize, VectorIcon.Flow, false);
            ImGui.SameLine();
            ImGui.Text(pin.Name);
            NodeEditor.EndPin();
        }

        StartFixed();
        if (NodeValueRenders.TryGetValue(Data.GetType(), out var renderer))
        {
            renderer(gameData, missionScript, ref nb.Popups, Data);
        }

        EndNodeLayout();

        // Explicitly dispose, a using statement breaks ref calls
        nb.Dispose();
    }
}
