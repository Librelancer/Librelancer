using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActRelocateShip : BlueprintNode
{
    protected override string Name => "Set Initial Player Position";

    public readonly Act_RelocateShip Data;
    public NodeActRelocateShip(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_RelocateShip(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        ImGui.InputFloat3("Position", ref Data.Position);
        // TODO: Orientation can be null?
        // Controls.InputFlQuaternion("Orientation", ref Data.Orientation);
    }
}
