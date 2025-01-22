using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActEtherComm : NodeTriggerEntry
{
    public override string Name => "Ether Comm";

    public readonly Act_EtherComm Data;
    public ActEtherComm(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_EtherComm(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        // TODO: FLESH OUT
        Controls.InputTextId("Line", ref Data.Line);
        Controls.InputTextId("Voices", ref Data.Voice);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
