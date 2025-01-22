using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActMovePlayer : NodeTriggerEntry
{
    public override string Name => "Move Player";

    public readonly Act_MovePlayer Data;
    public ActMovePlayer(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_MovePlayer(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        ImGui.InputFloat3("Position", ref Data.Position);
        ImGui.InputFloat("Unknown", ref Data.Unknown);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
