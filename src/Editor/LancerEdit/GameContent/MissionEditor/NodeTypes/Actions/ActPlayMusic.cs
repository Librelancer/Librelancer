using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Schema.Audio;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActPlayMusic : NodeTriggerEntry
{
    public override string Name => "Set Music";

    public readonly Act_PlayMusic Data;
    public ActPlayMusic(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_PlayMusic(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.CheckboxUndo("Reset to Default", undoBuffer, () => ref Data.Reset);

        nodePopups.StringCombo("Space", undoBuffer, () => ref Data.Space, gameData.MusicByName, true);
        ImGui.SameLine();
        if(Controls.Music("Space", gameData.Sounds, !string.IsNullOrWhiteSpace(Data.Space)))
            gameData.Sounds.PlayMusic(Data.Space, 0, true);
        nodePopups.StringCombo("Danger", undoBuffer, () => ref Data.Danger, gameData.MusicByName, true);
        ImGui.SameLine();
        if(Controls.Music("Danger", gameData.Sounds, !string.IsNullOrWhiteSpace(Data.Danger)))
            gameData.Sounds.PlayMusic(Data.Danger, 0, true);
        nodePopups.StringCombo("Battle", undoBuffer, () => ref Data.Battle, gameData.MusicByName, true);
        ImGui.SameLine();
        if(Controls.Music("Battle", gameData.Sounds, !string.IsNullOrWhiteSpace(Data.Battle)))
            gameData.Sounds.PlayMusic(Data.Battle, 0, true);
        nodePopups.StringCombo("Motif", undoBuffer, () => ref Data.Motif, gameData.MusicByName, true);
        ImGui.SameLine();
        if(Controls.Music("Motif", gameData.Sounds, !string.IsNullOrWhiteSpace(Data.Motif)))
            gameData.Sounds.PlayMusic(Data.Motif, 0, true);
        Controls.InputFloatUndo("Fade", undoBuffer, () => ref Data.Fade);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_PlayMusic,
            BuildEntry()
        );
    }
}
