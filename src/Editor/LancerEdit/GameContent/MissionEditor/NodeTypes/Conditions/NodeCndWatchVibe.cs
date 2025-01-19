using System;
using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

// i am not convinced that watch vibe works in the base game, needs testing

public class NodeCndWatchVibe : TriggerEntryNode
{
    protected override string Name => "On Vibe State Change (Reputation)";

    // How does the source feel about the target

    private Cnd_WatchVibe Data;
    public NodeCndWatchVibe(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        Data = entry is null ? new() : new Cnd_WatchVibe(entry);

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        Controls.InputTextId("Source Object", ref Data.SourceObject);
        Controls.InputTextId("Target Object", ref Data.TargetObject);

        NodeActSetVibe.VibeComboBox(ref Data.Vibe, nodePopups);
        nodePopups.Combo("Modifier", Data.ModifierIndex, i => Data.ModifierIndex = i, Cnd_WatchVibe.Options);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
