using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes;

public abstract class BlueprintNode : Node
{
    protected virtual float NodeInnerWidth => 200f;
    protected BlueprintNode(ref int id, VertexDiffuse? color = null) : base(id++, color)
    {
    }

    protected abstract void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni);

    public sealed override void Render(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        var iconSize  = new Vector2(24 * ImGuiHelper.Scale);
        var nb = NodeBuilder.Begin(Id);

        nb.Header(Color);
        ImGui.Text(Name);
        nb.EndHeader();

        LayoutNode(Inputs.Select(x => x.LinkType.ToString()), Outputs.Select(x => x.LinkType.ToString()), NodeInnerWidth * ImGuiHelper.Scale);
        StartInputs();

        foreach (var pin in Inputs)
        {
            NodeEditor.BeginPin(pin.Id, PinKind.Input);

            Color4 color = pin.LinkType switch
            {
                LinkType.Command => Color4.Cyan,
                LinkType.CommandList => Color4.DarkBlue,
                LinkType.Condition => Color4.Orange,
                LinkType.Trigger => Color4.Green,
                LinkType.Action => Color4.Red,
                _ => Color4.White,
            };

            VectorIcons.Icon(iconSize, VectorIcon.Flow, false, color);
            ImGui.SameLine();
            ImGui.Text(pin.LinkType.ToString());
            NodeEditor.EndPin();
        }

        StartOutputs();
        foreach (var pin in Outputs)
        {
            NodeEditor.BeginPin(pin.Id, PinKind.Output);
            VectorIcons.Icon(iconSize, VectorIcon.Flow, false);
            ImGui.SameLine();
            ImGui.Text(pin.LinkType.ToString());
            NodeEditor.EndPin();
        }

        StartFixed();

        RenderContent(gameData, popup, missionIni);

        EndNodeLayout();

        // Explicitly dispose, a using statement breaks ref calls
        nb.Dispose();
    }
}
