using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndBaseEnter : TriggerEntryNode
{
    protected override string Name => "On Base Enter";

    public Cnd_BaseEnter Data;
    public NodeCndBaseEnter(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);
        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        Controls.InputTextId("Base", ref Data.@base); // TODO: Comboify
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
    }
}
