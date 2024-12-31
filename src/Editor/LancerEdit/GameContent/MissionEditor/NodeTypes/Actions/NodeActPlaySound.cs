using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActPlaySound : BlueprintNode
{
    protected override string Name => "Play sound";

    private readonly Act_PlaySoundEffect data;
    public NodeActPlaySound(ref int id, Act_PlaySoundEffect data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Sound Id", ref data.Effect);
    }
}
