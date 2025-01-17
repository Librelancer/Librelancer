using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions.Conditions;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndTradeLaneExit : TriggerEntryNode
{
    protected override string Name => "On TL Exit";

    public Cnd_TLExited Data;
    public NodeCndTradeLaneExit(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);
        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        Controls.InputTextId("Source", ref Data.Source);
        Controls.InputTextId("Start Ring", ref Data.StartRing);
        Controls.InputTextId("Next Ring", ref Data.NextRing);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
