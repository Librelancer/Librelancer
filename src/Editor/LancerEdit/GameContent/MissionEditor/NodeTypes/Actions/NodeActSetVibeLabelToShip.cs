using System.Linq;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetVibeLabelToShip : TriggerEntryNode
{
    protected override string Name => "Set Vibe Label to Ship";

    public readonly Act_SetVibeLblToShip Data;

    public NodeActSetVibeLabelToShip(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetVibeLblToShip(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        var labels = missionIni.Ships.SelectMany(x => x.Labels).Order().ToArray();
        var ships = missionIni.Ships.Select(x => x.Nickname).Order().ToArray();

        NodeActSetVibe.VibeComboBox(ref Data.Vibe, nodePopups);
        nodePopups.StringCombo("Label", Data.Label, s => Data.Label = s, labels);
        nodePopups.StringCombo("Ship", Data.Ship, s => Data.Ship = s, ships);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
