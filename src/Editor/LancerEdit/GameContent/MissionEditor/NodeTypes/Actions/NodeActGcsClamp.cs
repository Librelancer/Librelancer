using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActGcsClamp : BlueprintNode
{
    protected override string Name => "Toggle GCS Clamp";

    private readonly Act_GcsClamp data;
    public NodeActGcsClamp(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_GcsClamp(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        ImGui.Checkbox("Enable", ref data.Clamp);
    }
}
