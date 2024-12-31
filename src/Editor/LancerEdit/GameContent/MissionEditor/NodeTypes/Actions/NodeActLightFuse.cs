using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActLightFuse : BlueprintNode
{
    protected override string Name => "Light Fuse";

    private readonly Act_LightFuse data;
    public NodeActLightFuse(ref int id, Act_LightFuse data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Fuse", ref data.Fuse);
        Controls.InputTextId("Target", ref data.Target);
    }
}
