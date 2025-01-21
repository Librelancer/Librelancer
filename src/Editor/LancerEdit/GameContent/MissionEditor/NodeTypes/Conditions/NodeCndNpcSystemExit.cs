using System;
using System.Collections.Generic;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;
public class NodeCndNpcSystemExit : TriggerEntryNode
{
    protected override string Name => "On NPC System Exit";

    public Cnd_NPCSystemExit Data;
    public NodeCndNpcSystemExit(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        ImGui.Text("This node type has not been tested. Proceed with caution.");
        Controls.InputTextId("System", ref Data.system);
        Controls.InputStringList("Ships", Data.ships);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
