using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActStartDialog : NodeTriggerEntry
{
    public override string Name => "Start Dialog";

    public readonly Act_StartDialog Data;
    public ActStartDialog(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_StartDialog(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.StringCombo("Dialog", Data.Dialog, s => Data.Dialog = s, lookups.Dialogs);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
