using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActMarkObject : BlueprintNode
{
    protected override string Name => "Mark Object";

    private readonly Act_MarkObj data;
    public NodeActMarkObject(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_MarkObj(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Object", ref data.Object);
        ImGui.InputInt("Value", ref data.Value); // TODO: An enum value of some kind
    }
}
