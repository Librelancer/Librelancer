using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActNagClamp : BlueprintNode
{
    protected override string Name => "Toggle Nag Clamp";

    private readonly Act_NagClamp data;
    public NodeActNagClamp(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_NagClamp(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        ImGui.Checkbox("Enable", ref data.Clamp);
    }
}
