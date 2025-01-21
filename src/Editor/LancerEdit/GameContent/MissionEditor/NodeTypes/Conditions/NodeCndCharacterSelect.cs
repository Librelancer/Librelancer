using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndCharacterSelect : TriggerEntryNode
{
    protected override string Name => "On Character Select";

    public Cnd_CharSelect Data;
    public NodeCndCharacterSelect(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
        Data = entry is null ? new() : new(entry);

    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextId("Character", ref Data.character);
        Controls.InputTextId("Location", ref Data.location);
        Controls.InputTextId("Base", ref Data.@base);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
