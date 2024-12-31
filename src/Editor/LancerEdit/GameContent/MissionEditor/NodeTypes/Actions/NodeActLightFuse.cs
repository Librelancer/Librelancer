using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActLightFuse : BlueprintNode
{
    protected override string Name => "Light Fuse";

    private readonly Act_LightFuse data;
    public NodeActLightFuse(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_LightFuse(action);
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Fuse", ref data.Fuse);
        Controls.InputTextId("Target", ref data.Target);
    }
}
