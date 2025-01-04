using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetNNState : BlueprintNode
{
    protected override string Name => "Set NN State";

    private readonly Act_SetNNState data;
    public NodeActSetNNState(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_SetNNState(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Objective", ref data.Objective);
        ImGui.Checkbox("Complete", ref data.Complete);
    }
}
