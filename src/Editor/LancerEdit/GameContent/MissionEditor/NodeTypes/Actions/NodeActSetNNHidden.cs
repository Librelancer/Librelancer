using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetNNHidden : BlueprintNode
{
    protected override string Name => "Set NN Hidden";

    public readonly Act_SetNNHidden Data;
    public NodeActSetNNHidden(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetNNHidden(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Objective", ref Data.Objective);
        ImGui.Checkbox("Hidden", ref Data.Hide);
    }
}
