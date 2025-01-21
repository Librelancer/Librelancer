using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndDestroyed : TriggerEntryNode
{
    protected override string Name => "On Object Destroyed";

    public Cnd_Destroyed Data;

    public NodeCndDestroyed(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.StringCombo("Target", Data.label, s => Data.label = s, lookups.ShipsAndLabels);

        ImGui.InputInt("Unknown Int", ref Data.UnknownNumber);
        Controls.InputTextId("Unknown Enum", ref Data.UnknownEnum);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
