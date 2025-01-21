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
        ref NodeLookups lookups)
    {

        Controls.InputTextId("Object", ref Data.Object);
        nodePopups.StringCombo("Faction", Data.Faction, s => Data.Faction = s, gameData.FactionsByName);
        nodePopups.Combo("Vibe", Data.VibeSet, x => Data.VibeSet = x);

        ImGui.BeginDisabled(Data.VibeSet != VibeSet.None);
        ImGui.SliderFloat("Value", ref Data.NewValue, -1, 1, "%.2f");
        ImGui.EndDisabled();
    }


    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
