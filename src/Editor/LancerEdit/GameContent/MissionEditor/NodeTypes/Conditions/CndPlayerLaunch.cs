using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndPlayerLaunch : NodeTriggerEntry
{
    public override string Name => "On Player Launch";

    public Cnd_PlayerLaunch Data = new();
    public CndPlayerLaunch(Entry entry): base(NodeColours.Condition)
    {
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition()
    {
        return new MissionCondition(
            TriggerConditions.Cnd_PlayerLaunch,
            BuildEntry()
        );
    }
    public override MissionAction CloneAction() => null;
}
