using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActStaticCamera : BlueprintNode
{
    protected override string Name => "Set Static Camera";

    public readonly Act_StaticCam Data;
    public NodeActStaticCamera(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_StaticCam(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        ImGui.InputFloat3("Position", ref Data.Position);
        Controls.InputFlQuaternion("Orientation", ref Data.Orientation);
    }
}
