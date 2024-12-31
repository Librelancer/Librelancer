using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActCloak : BlueprintNode
{
    protected override string Name => "Act Cloak";

    private readonly Act_Cloak data;
    public NodeActCloak(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_Cloak(action);
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Target", ref data.Target);
        ImGui.Checkbox("Cloak", ref data.Cloaked);
    }
}
