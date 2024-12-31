using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActForceLand : BlueprintNode
{
    protected override string Name => "Force Land";

    private readonly Act_ForceLand data;
    public NodeActForceLand(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_ForceLand(action);
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Base", ref data.Base);
    }
}
