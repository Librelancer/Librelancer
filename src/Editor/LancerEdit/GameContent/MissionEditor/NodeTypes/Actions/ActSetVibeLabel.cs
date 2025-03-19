using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSetVibeLabel : NodeTriggerEntry
{
    public override string Name => "Set Vibe Label";

    public readonly Act_SetVibeLbl Data;

    public ActSetVibeLabel(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetVibeLbl(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        ActSetVibe.VibeComboBox(ref Data.Vibe, nodePopups);
        nodePopups.StringCombo("Label 1", Data.Label1, s => Data.Label1 = s, lookups.Labels);
        nodePopups.StringCombo("Label 2", Data.Label2, s => Data.Label2 = s, lookups.Labels);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
