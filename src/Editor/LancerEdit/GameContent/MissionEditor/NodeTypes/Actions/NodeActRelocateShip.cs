using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActRelocateShip : BlueprintNode
{
    protected override string Name => "Set Initial Player Position";

    private readonly Act_RelocateShip data;
    public NodeActRelocateShip(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_RelocateShip(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        ImGui.InputFloat3("Position", ref data.Position);
        // TODO: Orientation can be null?
        // Controls.InputFlQuaternion("Orientation", ref data.Orientation);
    }
}
