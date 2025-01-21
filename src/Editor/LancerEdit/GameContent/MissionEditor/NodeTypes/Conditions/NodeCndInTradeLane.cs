using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndInTradeLane : TriggerEntryNode
{
    protected override string Name => "In Trade Lane";

    private Cnd_InTradelane Data;
    public NodeCndInTradeLane(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        ImGui.Checkbox("In Trade Lane", ref Data.inTL);
        nodePopups.StringCombo("Ship", Data.Ship, s => Data.Ship = s, lookups.Ships);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
