using System;
using System.Linq;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSendComm : NodeTriggerEntry
{
    public override string Name => "Send Comm";

    public readonly Act_SendComm Data;
    public ActSendComm(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SendComm(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Source", undoBuffer, () => ref Data.Source);
        Controls.InputTextIdUndo("Destination", undoBuffer, () => ref Data.Destination);
        Controls.InputTextIdUndo("Line", undoBuffer, () => ref Data.Line);

        if (!ImGui.Button(Icons.Play + " Play Line"))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(Data.Source) &&
            lookups.MissionIni.Ships.TryGetValue(Data.Source, out var source)
            && !string.IsNullOrWhiteSpace(source?.NPC?.Voice))
        {
            gameData.Sounds.PlayVoiceLine(source.NPC.Voice, Data.Line);
            return;
        }

        if (!string.IsNullOrWhiteSpace(Data.Source) &&
            lookups.MissionIni.Solars.TryGetValue(Data.Source, out var source2) &&
            !string.IsNullOrWhiteSpace(source2.Voice))
        {
            gameData.Sounds.PlayVoiceLine(source2.Voice, Data.Line);
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
            TriggerActions.Act_SendComm,
            BuildEntry()
        );
    }
}
