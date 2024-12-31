using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActEtherComm : BlueprintNode
{
    protected override string Name => "Ether Comm";

    private readonly Act_EtherComm data;
    public NodeActEtherComm(ref int id, Act_EtherComm data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Line", ref data.Line);
        Controls.InputTextId("Voices", ref data.Voice);
    }
}
