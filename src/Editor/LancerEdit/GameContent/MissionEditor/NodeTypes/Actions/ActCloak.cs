using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActCloak : NodeTriggerEntry
{
    public override string Name => "Act Cloak";

    public readonly Act_Cloak Data;
    public ActCloak(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_Cloak(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.StringCombo("Ship", Data.Target, s => Data.Target = s, lookups.Ships);
        ImGui.Checkbox("Cloak", ref Data.Cloaked);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
