using System;
using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;
using LibreLancer.Missions.Directives;
using LibreLancer.World;

namespace LancerEdit.GameContent.MissionEditor;

public static class DirectiveEditor
{
    public static void EditDirective(
        MissionDirective directive,
        EditorUndoBuffer undoBuffer)
    {
        switch (directive)
        {
            case AvoidanceDirective avoidance:
                EditAvoidance(avoidance, undoBuffer);
                break;
            case BreakFormationDirective:
                ImGui.Text("no_params");
                break;
            case DelayDirective delay:
                EditDelay(delay, undoBuffer);
                break;
            case DockDirective dock:
                EditDock(dock, undoBuffer);
                break;
            case FollowDirective follow:
                EditFollow(follow, undoBuffer);
                break;
            case FollowPlayerDirective followPlayer:
                EditFollowPlayer(followPlayer, undoBuffer);
                break;
            case GotoShipDirective gotoShip:
                EditGotoShip(gotoShip, undoBuffer);
                break;
            case GotoSplineDirective gotoSpline:
                EditGotoSpline(gotoSpline, undoBuffer);
                break;
            case GotoVecDirective gotoVec:
                EditGotoVec(gotoVec, undoBuffer);
                break;
            case IdleDirective:
                ImGui.Text("no_params");
                break;
            case MakeNewFormationDirective newFormation:
                EditMakeNewFormation(newFormation, undoBuffer);
                break;
            case SetPriorityDirective setPriority:
                EditSetPriority(setPriority, undoBuffer);
                break;
            case SetLifetimeDirective setLifetime:
                EditSetLifetime(setLifetime, undoBuffer);
                break;
            case StayInRangeDirective stayInRange:
                EditStayInRange(stayInRange, undoBuffer);
                break;
            case StayOutOfRangeDirective stayOutRange:
                EditStayOutOfRange(stayOutRange, undoBuffer);
                break;
            default:
                throw new InvalidOperationException();
        }
    }

    static void EditAvoidance(AvoidanceDirective directive, EditorUndoBuffer undoBuffer)
    {
        Controls.CheckboxUndo("Avoidance", undoBuffer, () => ref directive.Avoidance);
    }

    static void EditDelay(DelayDirective directive, EditorUndoBuffer undoBuffer)
    {
        Controls.InputFloatUndo("Time", undoBuffer, () => ref directive.Time);
    }

    static void EditDock(DockDirective directive, EditorUndoBuffer undoBuffer)
    {
        Controls.InputTextIdUndo("Target",  undoBuffer, () => ref directive.Target);
        Controls.InputTextIdUndo("Towards", undoBuffer, () => ref directive.Towards);
    }

    static void EditFollow(FollowDirective directive, EditorUndoBuffer undoBuffer)
    {
        Controls.InputTextIdUndo("Target",  undoBuffer, () => ref directive.Target);
        Controls.InputFloatUndo("Range 0", undoBuffer, () => ref directive.Range0);
        Controls.InputFloat3Undo("Offset", undoBuffer, () => ref directive.Offset, "%.0f");
        Controls.InputFloatUndo("Range 1", undoBuffer, () => ref directive.Range1);
    }

    static void EditFollowPlayer(FollowPlayerDirective directive, EditorUndoBuffer undoBuffer)
    {
        Controls.InputTextIdUndo("Formation", undoBuffer, () => ref directive.Formation);
        Controls.InputStringList("Ships",  undoBuffer, directive.Ships);
    }

    static void EditGotoShip(GotoShipDirective directive, EditorUndoBuffer undoBuffer)
    {
        GotoKindUndo(() => ref directive.CruiseKind, undoBuffer);
        Controls.InputTextIdUndo("Target", undoBuffer, () => ref directive.Target);
        Controls.InputFloatUndo("Distance", undoBuffer, () => ref directive.Range);
        Controls.CheckboxUndo("Unknown", undoBuffer, () => ref directive.Unknown);
        Controls.InputFloatUndo("Max Throttle", undoBuffer, () => ref directive.MaxThrottle);
    }

    static void EditGotoSpline(GotoSplineDirective directive, EditorUndoBuffer undoBuffer)
    {
        GotoKindUndo(() => ref directive.CruiseKind, undoBuffer);
        Controls.InputFloat3Undo("A", undoBuffer, () => ref directive.PointA, "%.0f");
        Controls.InputFloat3Undo("B", undoBuffer, () => ref directive.PointB, "%.0f");
        Controls.InputFloat3Undo("C", undoBuffer, () => ref directive.PointC, "%.0f");
        Controls.InputFloat3Undo("D", undoBuffer, () => ref directive.PointD, "%.0f");
        Controls.InputFloatUndo("Distance", undoBuffer, () => ref directive.Range);
        Controls.CheckboxUndo("Unknown", undoBuffer, () => ref directive.Unknown);
        Controls.InputFloatUndo("Max Throttle", undoBuffer, () => ref directive.MaxThrottle);
    }

    static void EditGotoVec(GotoVecDirective directive, EditorUndoBuffer undoBuffer)
    {
        GotoKindUndo(() => ref directive.CruiseKind, undoBuffer);
        Controls.InputFloat3Undo("Offset", undoBuffer, () => ref directive.Target, "%.0f");
        Controls.InputFloatUndo("Distance", undoBuffer, () => ref directive.Range);
        Controls.CheckboxUndo("Unknown 2", undoBuffer, () => ref directive.Unknown);
        Controls.InputFloatUndo("Max Throttle", undoBuffer, () => ref directive.MaxThrottle);
    }

    static void EditMakeNewFormation(MakeNewFormationDirective directive, EditorUndoBuffer undoBuffer)
    {
        Controls.InputTextIdUndo("Formation", undoBuffer, () => ref directive.Formation);
        Controls.InputStringList("Ships",  undoBuffer, directive.Ships);
    }

    static void EditSetPriority(SetPriorityDirective directive, EditorUndoBuffer undoBuffer)
    {
        var v = directive.AlwaysExecute;
        ImGuiExt.ButtonDivided("prio", "ALWAYS_EXECUTE", "NORMAL", ref v);
        if (v != directive.AlwaysExecute)
            undoBuffer.Set("ALWAYS_EXECUTE", () => ref directive.AlwaysExecute, v);
    }

    static void EditSetLifetime(SetLifetimeDirective directive, EditorUndoBuffer undoBuffer)
    {
        Controls.InputFloatUndo("Lifetime", undoBuffer, () => ref directive.Lifetime, null, "%.0f");
    }

    static void EditStayInRange(StayInRangeDirective directive, EditorUndoBuffer undoBuffer)
    {
        if (ImGui.RadioButton("Use Object", directive.UseObject) && !directive.UseObject)
            undoBuffer.Set("Use Object", () => ref directive.UseObject, true);
        if(ImGui.RadioButton("Use Point", !directive.UseObject) && directive.UseObject)
            undoBuffer.Set("Use Object",  () => ref directive.UseObject, false);
        if (directive.UseObject)
            Controls.InputTextIdUndo("Object", undoBuffer, () => ref directive.Object);
        else
            Controls.InputFloat3Undo("Point", undoBuffer, () => ref directive.Point);
        Controls.InputFloatUndo("Distance", undoBuffer, () => ref directive.Range);
        Controls.CheckboxUndo("Unknown", undoBuffer, () => ref directive.Unknown);
    }

    static void EditStayOutOfRange(StayOutOfRangeDirective directive, EditorUndoBuffer undoBuffer)
    {
        if (ImGui.RadioButton("Use Object", directive.UseObject) && !directive.UseObject)
            undoBuffer.Set("Use Object", () => ref directive.UseObject, true);
        if(ImGui.RadioButton("Use Point", !directive.UseObject) && directive.UseObject)
            undoBuffer.Set("Use Object",  () => ref directive.UseObject, false);
        if (directive.UseObject)
            Controls.InputTextIdUndo("Object", undoBuffer, () => ref directive.Object);
        else
            Controls.InputFloat3Undo("Point", undoBuffer, () => ref directive.Point);
        Controls.InputFloatUndo("Distance", undoBuffer, () => ref directive.Range);
        Controls.CheckboxUndo("Unknown", undoBuffer, () => ref directive.Unknown);
    }

    static void GotoKindUndo(FieldAccessor<GotoKind> accessor, EditorUndoBuffer undoBuffer)
    {
        var gotoKind = accessor();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Goto Kind");
        ImGui.SameLine();
        if(ImGui.BeginCombo("##gotoKind",  gotoKind.ToString()))
        {
            if(ImGui.Selectable("Goto") && gotoKind != GotoKind.Goto)
                undoBuffer.Set("Goto Kind", accessor, GotoKind.Goto);
            if(ImGui.Selectable("GotoNoCruise") && gotoKind != GotoKind.GotoNoCruise)
                undoBuffer.Set("Goto Kind", accessor, GotoKind.GotoNoCruise);
            if(ImGui.Selectable("Goto Cruise") && gotoKind != GotoKind.GotoCruise)
                undoBuffer.Set("Goto Kind", accessor, GotoKind.GotoCruise);
            ImGui.EndCombo();
        }
    }
}
