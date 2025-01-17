using System.Linq;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActStartDialog : BlueprintNode
{
    protected override string Name => "Start Dialog";

    public readonly Act_StartDialog Data;
    public NodeActStartDialog(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_StartDialog(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        var dialogs = missionIni.Dialogs.Select(x => x.Nickname).ToArray();

        nodePopups.StringCombo("Dialog", Data.Dialog, s => Data.Dialog = s, dialogs);
    }
}
