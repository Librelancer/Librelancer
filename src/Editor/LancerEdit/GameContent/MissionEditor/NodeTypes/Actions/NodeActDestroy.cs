using System.Linq;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActDestroy : BlueprintNode
{
    protected override string Name => "Destroy";

    public readonly Act_Destroy Data;
    public NodeActDestroy(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_Destroy(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        var targets = missionIni.Ships.Select(x => x.Nickname).Concat(missionIni.Solars.Select(x => x.Nickname)).ToArray();
        nodePopups.StringCombo("Target", Data.Target, s => Data.Target = s, targets);
    }
}
