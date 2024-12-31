using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActPlaySound : BlueprintNode
{
    protected override string Name => "Play sound";

    private readonly Act_PlaySoundEffect data;
    public NodeActPlaySound(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_PlaySoundEffect(action);
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Sound Id", ref data.Effect);
    }
}
