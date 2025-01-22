using System.Linq;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSetVibeShipToLabel : NodeTriggerEntry
{
    public override string Name => "Set Vibe Ship to Label";

    public readonly Act_SetVibeShipToLbl Data;

    public ActSetVibeShipToLabel(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetVibeShipToLbl(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {

        ActSetVibe.VibeComboBox(ref Data.Vibe, nodePopups);
        nodePopups.StringCombo("Ship", Data.Ship, s => Data.Ship = s, lookups.Ships);
        nodePopups.StringCombo("Label", Data.Label, s => Data.Label = s, lookups.Labels);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
