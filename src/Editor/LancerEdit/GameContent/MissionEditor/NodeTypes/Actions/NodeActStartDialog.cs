using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActStartDialog : BlueprintNode
{
    protected override string Name => "Start Dialog";

    private readonly Act_StartDialog data;
    public NodeActStartDialog(ref int id, Act_StartDialog data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Dialog", ref data.Dialog);
    }
}
