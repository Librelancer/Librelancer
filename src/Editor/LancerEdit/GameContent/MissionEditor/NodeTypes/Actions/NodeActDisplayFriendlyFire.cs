using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActDisableFriendlyFire : BlueprintNode
{
    protected override string Name => "Disable Friendly Fire";

    private readonly Act_DisableFriendlyFire data;
    public NodeActDisableFriendlyFire(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_DisableFriendlyFire(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputStringList("Objects & Labels", data.ObjectsAndLabels);
    }
}
