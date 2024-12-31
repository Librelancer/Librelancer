using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

// ReSharper disable once InconsistentNaming
public sealed class NodeActSetNNObject : BlueprintNode
{
    protected override string Name => "Set NN Object";

    private readonly Act_SetNNObj data;
    public NodeActSetNNObject(ref int id, Act_SetNNObj data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Objective", ref data.Objective);
    }
}
