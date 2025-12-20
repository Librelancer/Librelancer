using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActNagDistLeaving : NodeTriggerEntry
{
    public override string Name => "Nag Dist Leaving";

    private bool isObject = false;
    private string savedTarget = "";
    public readonly Act_NagDistLeaving Data;
    public ActNagDistLeaving(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_NagDistLeaving(action);

        isObject = !string.IsNullOrWhiteSpace(Data.Target);
        savedTarget = Data.Target ?? "";

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    class SetObjectAction(ActNagDistLeaving node) : EditorAction
    {
        public override void Commit()
        {
            node.isObject = true;
            node.Data.Target = node.savedTarget;
        }

        public override void Undo()
        {
            node.isObject = false;
            node.Data.Target = "";
        }
        public override string ToString() => "Point->Object";
    }

    class SetPointAction(ActNagDistLeaving node) : EditorAction
    {
        public override void Commit()
        {
            node.savedTarget = node.Data.Target;
            node.isObject = false;
            node.Data.Target = "";
        }

        public override void Undo()
        {
            node.isObject = true;
            node.Data.Target = node.savedTarget;
        }

        public override string ToString() => "Object->Point";
    }



    private static readonly string[] _nags = Enum.GetValues<NagType>().Select(x => x.ToString()).Append("NULL").ToArray();
    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Nickname", undoBuffer, () => ref Data.Nickname);
        Controls.InputTextIdUndo("Nagger", undoBuffer, () => ref Data.Nagger);
        Controls.IdsInputStringUndo("Mission Fail IDS", gameData, popup, undoBuffer,
            () => ref Data.MissionFailIds);
        Controls.InputFloatUndo("Distance", undoBuffer, () => ref Data.Distance);
        nodePopups.Combo("Nag Type", undoBuffer, () => ref Data.NagType);
        if (ImGui.RadioButton("Use Target", isObject) && !isObject)
            undoBuffer.Commit(new SetObjectAction(this));
        if(ImGui.RadioButton("Use Position", !isObject) && isObject)
            undoBuffer.Commit(new SetPointAction(this));
        if (isObject)
            Controls.InputTextIdUndo("Target", undoBuffer, () => ref Data.Target);
        else
            Controls.InputFloat3Undo("Position", undoBuffer, () => ref Data.Position);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
