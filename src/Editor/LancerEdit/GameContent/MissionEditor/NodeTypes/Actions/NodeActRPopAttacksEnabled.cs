using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActRPopAttacksEnabled : BlueprintNode
{
    protected override string Name => "Toggle Random Pop Attacks";

    private readonly Act_RpopTLAttacksEnabled data;
    public NodeActRPopAttacksEnabled(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_RpopTLAttacksEnabled(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        ImGui.Checkbox("Enable", ref data.Enabled);
    }
}
