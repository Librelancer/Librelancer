using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActForceLand : BlueprintNode
{
    protected override string Name => "Force Land";

    private readonly Act_ForceLand data;
    public NodeActForceLand(ref int id, Act_ForceLand data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Base", ref data.Base);
    }
}
