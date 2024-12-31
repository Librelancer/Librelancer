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

public abstract class BlueprintNode : Node
{
    protected BlueprintNode(ref int id, VertexDiffuse? color = null) : base(id++, color)
    {
    }

    protected abstract void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript);

    public sealed override void Render(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
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

        RenderContent(gameData, popup, missionScript);

        EndNodeLayout();

        // Explicitly dispose, a using statement breaks ref calls
        nb.Dispose();
    }
}
