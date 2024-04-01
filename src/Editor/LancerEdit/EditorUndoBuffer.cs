using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;

namespace LancerEdit;

public class EditorUndoBuffer
{
    private Stack<EditorAction> undoStack = new Stack<EditorAction>();
    private Stack<EditorAction> redoStack = new Stack<EditorAction>();

    public void Commit(EditorAction action)
    {
        action.Commit();
        undoStack.Push(action);
        redoStack.Clear();
    }

    public void Push(EditorAction action)
    {
        undoStack.Push(action);
        redoStack.Clear();
    }

    public void Undo()
    {
        var item = undoStack.Pop();
        item.Undo();
        redoStack.Push(item);
    }

    public void Redo()
    {
        var item = redoStack.Pop();
        item.Commit();
        undoStack.Push(item);
    }


    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;

    public void DisplayStack()
    {
        ImGui.SetNextWindowSize(new Vector2(350) * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Command History"))
        {
            ImGui.BeginTable("##items", 1, ImGuiTableFlags.Borders);
            ImGui.PushStyleColor(ImGuiCol.Text, Color4.Gray);
            foreach (var item in redoStack.Reverse())
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(item.ToString());
            }
            ImGui.PopStyleColor();
            foreach (var item in undoStack)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(item.ToString());
            }
            ImGui.EndTable();
        }
        ImGui.End();
    }
}

public abstract class EditorAction
{
    public abstract void Commit();
    public abstract void Undo();
}

public class EditorAggregateAction : EditorAction
{
    private EditorAction[] actions;
    private EditorAggregateAction()
    {
    }

    public static EditorAction Create(EditorAction[] actions)
    {
        if (actions.Length == 1)
            return actions[0];
        return new EditorAggregateAction() { actions = actions };
    }

    public override void Commit()
    {
        foreach(var a in actions)
            a.Commit();
    }

    public override void Undo()
    {
        foreach(var a in actions)
            a.Undo();
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Aggregate ({actions.Length})");
        foreach (var a in actions)
            builder.AppendLine(a.ToString());
        builder.Append("--");
        return builder.ToString();
    }
}

public abstract class EditorModification<T>(T old, T updated) : EditorAction
{
    public T Old = old;
    public T Updated = updated;

    public abstract void Set(T value);

    public override void Commit() => Set(Updated);
    public override void Undo() => Set(Old);
}
