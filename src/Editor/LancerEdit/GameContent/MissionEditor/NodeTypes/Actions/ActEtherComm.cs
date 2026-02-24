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

public sealed class ActEtherComm : NodeTriggerEntry
{
    public override string Name => "Ether Comm";

    public readonly Act_EtherComm Data;
    public ActEtherComm(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_EtherComm(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        // TODO: FLESH OUT
        Controls.InputTextIdUndo("Line", undoBuffer, () => ref Data.Line);
        Controls.InputTextIdUndo("Voices", undoBuffer, () => ref Data.Voice);

        if (ImGui.Button("Play Line " + Icons.Play))
        {
            gameData.Sounds.PlayVoiceLine(Data.Voice, Data.Line);
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
            TriggerActions.Act_EtherComm,
            BuildEntry()
        );
    }
}
