using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndPlayerManeuver : NodeTriggerEntry
{
    public override string Name => "On Player Maneuver";

    public Cnd_PlayerManeuver Data;
    public CndPlayerManeuver(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    private readonly string[] maneuverTypes = Enum.GetNames<ManeuverType>();
    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
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
