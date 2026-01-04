using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;

namespace LancerEdit.GameContent.Lookups;

public class ObjectLookup<T> where T : class
{
    protected T[] Options;

    private Func<T, string> displayName;

    public ObjectLookup(
        IEnumerable<T> values,
        Func<T,string> name = null)
    {
        Options = values.ToArray();
        displayName = name;
    }

    public ObjectLookup<T> Filter(Func<T, bool> filter) =>
        new(Options.Where(filter), displayName);

    public bool DrawUndo(string label,
        EditorUndoBuffer undoBuffer,
        FieldAccessor<T> accessor,
        bool allowNull = false)
    {
        ref var sel = ref accessor();
        return Draw(label, ref sel, (o, u) => undoBuffer.Set(label, accessor, o, u));
    }

    public bool Draw(string label,
        ref T selected,
        Action<T, T> onSelected = null,
        bool allowNull = false)
    {
        return Draw(label, ref selected, out _, onSelected, allowNull);
    }


    public bool Draw(string label,
        ref T selected,
        out T hovered,
        Action<T, T> onSelected = null,
        bool allowNull = false)
    {
        if (Controls.InEditorTable)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
        }
        if (label != null &&
            !label.StartsWith("##"))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);
        }
        if (Controls.InEditorTable)
        {
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
        }

        return SearchDropdown<T>.Draw(
            label, ref selected, out hovered,
            Options, displayName, onSelected, allowNull);
    }
}
