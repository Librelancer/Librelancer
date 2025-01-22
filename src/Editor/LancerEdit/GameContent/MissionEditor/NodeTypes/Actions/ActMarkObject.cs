using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActMarkObject : NodeTriggerEntry
{
    public override string Name => "Mark Object";

    public readonly Act_MarkObj Data;
    public ActMarkObject(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_MarkObj(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextId("Object", ref Data.Object);
        ImGui.InputInt("Value", ref Data.Value); // TODO: An enum value of some kind
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
