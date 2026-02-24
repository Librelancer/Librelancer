using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Media;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActPlaySound : NodeTriggerEntry
{
    public override string Name => "Play sound";

    public readonly Act_PlaySoundEffect Data;
    public ActPlaySound(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_PlaySoundEffect(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Sound Id", undoBuffer, () => ref Data.Effect);
        var sound = gameData.GameData.AllSounds.FirstOrDefault(x => x.Nickname.Equals(Data.Effect, StringComparison.OrdinalIgnoreCase));

        if (sound is not null)
        {
            if (ImGui.Button("Play Sound " + Icons.Play))
            {
                gameData.Sounds.PlayOneShot(sound.Nickname);
            }
        }
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_PlaySoundEffect,
            BuildEntry()
        );
    }
}
