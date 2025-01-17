using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActDisableFriendlyFire : BlueprintNode
{
    protected override string Name => "Disable Friendly Fire";

    public readonly Act_DisableFriendlyFire Data;
    public NodeActDisableFriendlyFire(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_DisableFriendlyFire(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        Controls.InputStringList("Objects & Labels", Data.ObjectsAndLabels);
    }
}
