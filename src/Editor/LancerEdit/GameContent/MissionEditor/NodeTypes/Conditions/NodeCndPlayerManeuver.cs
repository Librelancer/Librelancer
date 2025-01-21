using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndPlayerManeuver : TriggerEntryNode
{
    protected override string Name => "On Player Maneuver";

    public Cnd_PlayerManeuver Data;
    public NodeCndPlayerManeuver(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    private readonly string[] maneuverTypes = Enum.GetNames<ManeuverType>();
    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        var index = (int)Data.type;
        nodePopups.Combo("Maneuver", index, i => Data.type = (ManeuverType)i, maneuverTypes);

        // TODO: transform this into a combobox of different ships or a object depending on type
        Controls.InputTextId("Target", ref Data.target);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
