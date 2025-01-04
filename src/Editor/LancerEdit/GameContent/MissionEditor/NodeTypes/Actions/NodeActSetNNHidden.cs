using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetNNHidden : BlueprintNode
{
    protected override string Name => "Set NN Hidden";

    private readonly Act_SetNNHidden data;
    public NodeActSetNNHidden(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_SetNNHidden(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Objective", ref data.Objective);
        ImGui.Checkbox("Hidden", ref data.Hide);
    }
}
