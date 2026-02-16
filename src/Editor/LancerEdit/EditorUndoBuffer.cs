using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.ImUI;

namespace LancerEdit;

public class EditorUndoBuffer
{
    private Stack<EditorAction> undoStack = new Stack<EditorAction>();
    private Stack<EditorAction> redoStack = new Stack<EditorAction>();

    public Action Hook;

    public void Commit(EditorAction action)
    {
        action.Commit();
        undoStack.Push(action);
        redoStack.Clear();
        Hook?.Invoke();
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
        Hook?.Invoke();
    }

    public void Redo()
    {
        var item = redoStack.Pop();
        item.Commit();
        undoStack.Push(item);
        Hook?.Invoke();
    }

    public void Set<T>(string name, FieldAccessor<T> accessor, T newValue, Action hook = null)
    {
        Commit(EditorPropertyModification<T>.Create(name, accessor, newValue, hook));
    }

    public void Set<T>(string name, FieldAccessor<T> accessor, T oldValue, T newValue, Action hook = null)
    {
        Commit(EditorPropertyModification<T>.Create(name, accessor, oldValue, newValue, hook));
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

public sealed class EditorNopAction : EditorAction
{
    public string Description;
    public EditorNopAction(string desc)
    {
        Description = desc;
    }
    public override void Commit() { }

    public override void Undo() { }

    public override string ToString() => Description;
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

    public override string ToString() => GetType().Name;
}

public abstract class EditorFlagModification<T, TFlag>(TFlag flag, bool newValue) : EditorAction where TFlag : struct, Enum
{
    public abstract ref TFlag Field { get; }

    public override void Commit()
    {
        if (newValue)
            MathHelper.SetFlag(ref Field, flag);
        else
            MathHelper.UnsetFlag(ref Field, flag);
    }

    public override void Undo()
    {
        if(newValue)
            MathHelper.UnsetFlag(ref Field, flag);
        else
            MathHelper.SetFlag(ref Field, flag);
    }
}

public delegate ref T FieldAccessor<T>();


public class EditorPropertyModification<T> : EditorModification<T>
{
    private string name;
    private FieldAccessor<T> accessor;
    private Action hook;

    private EditorPropertyModification(string name, T old, T updated, FieldAccessor<T> accessor, Action hook)
        : base(old, updated)
    {
        this.name = name;
        this.accessor = accessor;
        this.hook = null;
    }


    public override void Set(T value)
    {
        accessor() = value;
        hook?.Invoke();
    }

    string Print(T obj) => obj?.ToString() ?? "NULL";

    public override string ToString() => $"{name}\nOld: {Print(Old)}\nUpdated: {Print(Updated)}";

    public static EditorPropertyModification<T> Create(string name, FieldAccessor<T> accessor, T updated,
        Action hook = null)
        => new(name, accessor(), updated, accessor, hook);

    public static EditorPropertyModification<T> Create(string name, FieldAccessor<T> accessor, T old, T updated,
        Action hook = null)
        => new(name, old, updated, accessor, hook);
}

public class ListAdd<T>(string Name, List<T> List, T Value) : EditorAction
{
    public override void Commit() => List.Add(Value);

    public override void Undo() => List.RemoveAt(List.Count - 1);

    public override string ToString() => $"{Name} Add Item";
}

public class ListSet<T>(string Name, List<T> List, int Index, T Old, T New)
    : EditorModification<T>(Old, New)
{
    public override void Set(T value) => List[Index] = value;

    string Print(T obj) => obj?.ToString() ?? "NULL";
    public override string ToString() => $"{Name}[{Index}] '{Print(Old)}'->'{Print(New)}'";
}

public class ListRemove<T>(string Name, List<T> List, int Index, T Value) : EditorAction
{
    public override void Commit() => List.RemoveAt(Index);

    public override void Undo() => List.Insert(Index, Value);

    public override string ToString() => $"{Name} Remove Item";
}

public class ItemRename<T>(string Old, string Updated, SortedDictionary<string, T> Collection, T Value)
    : EditorAction where T : NicknameItem
{
    public override void Commit()
    {
        Collection.Remove(Old);
        Value.Nickname = Updated;
        Collection.Add(Updated, Value);
    }

    public override void Undo()
    {
        Value.Nickname = Old;
        Collection.Remove(Updated);
        Collection.Add(Old, Value);
    }

    public override string ToString() => $"Rename '{Old}'->'{Updated}'";
}

public class DictionaryAdd<T>(string Name, SortedDictionary<string, T> Collection, T Value,
    FieldAccessor<T> selected)
    : EditorAction where T : NicknameItem
{
    public override void Commit()
    {
        selected() = Value;
        Collection.Add(Value.Nickname, Value);
    }

    public override void Undo()
    {
        if (selected() == Value)
            selected() = null;
        Collection.Remove(Value.Nickname);
    }

    public override string ToString() => $"{Name} Add Item";
}

public class DictionaryRemove<T>(string Name, SortedDictionary<string, T> Collection, T Value,
    FieldAccessor<T> selected)
    : EditorAction where T : NicknameItem
{
    public override void Commit()
    {
        if (selected() == Value)
            selected() = null;
        Collection.Remove(Value.Nickname);
    }

    public override void Undo()
    {
        if (selected() == null)
            selected() = Value;
        Collection.Add(Value.Nickname, Value);
    }

    public override string ToString() => $"{Name} Delete Item";
}
