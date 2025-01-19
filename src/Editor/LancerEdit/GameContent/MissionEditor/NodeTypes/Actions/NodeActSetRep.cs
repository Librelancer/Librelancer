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

public sealed class NodeActSetRep : TriggerEntryNode
{
    protected override string Name => "Set Reputation";

    public readonly Act_SetRep Data;

    public NodeActSetRep(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetRep(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        var factions = gameData.GameData.Factions.Select(x => x.Nickname).OrderBy(x => x).Order().ToArray();

        Controls.InputTextId("Object", ref Data.Object);
        nodePopups.StringCombo("Faction", Data.Faction, s => Data.Faction = s, factions);
        VibeComboBox(ref Data.VibeSet, nodePopups);

        ImGui.BeginDisabled(Data.VibeSet != VibeSet.None);
        ImGui.SliderFloat("Value", ref Data.NewValue, -1, 1, "%.2f");
        ImGui.EndDisabled();
    }

    private static readonly string[] _vibeList = Enum.GetValues<VibeSet>().Select(x => x.ToString()).ToArray();
    public static void VibeComboBox(ref VibeSet vibeSet, NodePopups nodePopups)
    {
        var index = (int)vibeSet;
        nodePopups.Combo("Vibe", index, i => index = i, _vibeList);
        vibeSet = (VibeSet)index;
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
