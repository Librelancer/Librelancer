using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActEnableManeuver : TriggerEntryNode
{
    protected override string Name => "Enable Maneuver";

    public readonly Act_EnableManeuver Data;
    public NodeActEnableManeuver(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_EnableManeuver(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    private static readonly string[] _maneuvers = Enum.GetValues<ManeuverType>().Select(x => x.ToString()).ToArray();
    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        var index = (int)Data.Maneuver;
        nodePopups.Combo("Maneuver", index, i => Data.Maneuver = (ManeuverType)i, _maneuvers);
        ImGui.Checkbox("Lock", ref Data.Lock);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
