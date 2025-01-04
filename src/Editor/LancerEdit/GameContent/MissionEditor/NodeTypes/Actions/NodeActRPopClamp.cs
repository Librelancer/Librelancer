using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActRPopClamp : BlueprintNode
{
    protected override string Name => "Toggle Random Pop Clamp";

    private readonly Act_RpopAttClamp data;
    public NodeActRPopClamp(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_RpopAttClamp(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        ImGui.Checkbox("Enable", ref data.Enabled);
    }
}
