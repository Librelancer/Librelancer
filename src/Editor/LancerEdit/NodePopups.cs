using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;

namespace LancerEdit;

public class NodeSuspendState
{
    private int waitFrames = 0;
    public void FlagSuspend()
    {
        waitFrames = 5;
    }
    public bool ShouldSuspend()
    {
        if (waitFrames > 0)
        {
            waitFrames--;
            return true;
        }
        return false;
    }
}

public struct NodePopups
{
    private string setTooltip;
    private int comboIndex;
    private ComboData[] combos;
    private NodeSuspendState suspend;
    record struct ComboData(bool Open, Action<int> Set, string Id, string[] Values);

    private StringComboData[] strCombos;
    private int strComboIndex;
    record struct StringComboData(bool Open, Action<string> Set, string Id, IEnumerable<string> Values, bool AllowEmpty);

    public NodeId CurrentId;

    public static NodePopups Begin(NodeId id, NodeSuspendState suspend) => new()
    {
        CurrentId = id,
        combos = ArrayPool<ComboData>.Shared.Rent(16),
        strCombos = ArrayPool<StringComboData>.Shared.Rent(16),
        suspend = suspend
    };

    public void Tooltip(string tooltip)
    {
        setTooltip = tooltip;
    }

    private void Combo(string title, int selectedValue, Action<int> set, string[] values)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text(title);
        ImGui.SameLine();

        // Hidden ID
        ImGuiExt.ComboButton($"##{title}", values[selectedValue]);
        var id = ImGui.GetItemID();
        bool activated = ImGui.IsItemActivated();

        combos[comboIndex++] = new ComboData(
            activated,
            set,
            $"{title}##{id}",
            values
        );
    }

    public void StringCombo(string title, EditorUndoBuffer undoBuffer, FieldAccessor<string> accessor, string[] values, bool allowEmpty = false)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text(title);
        ImGui.SameLine();

        var selectedValue = accessor();

        var display = values.FirstOrDefault(x => x.Equals(selectedValue, StringComparison.OrdinalIgnoreCase)) ?? selectedValue;
        if (allowEmpty && string.IsNullOrEmpty(display))
        {
            display = "(none)";
        }

        bool clicked = ImGuiExt.ComboButton($"##{title}", display);
        var id = ImGui.GetItemID();
        bool activated = ImGui.IsItemActivated();


        strCombos[strComboIndex++] = new StringComboData(
            activated,  // NOT clicked/open
            updated => undoBuffer.Set(title, accessor, updated),
            $"{title}##{id}",
            values,
            allowEmpty
        );
    }

    private static readonly Dictionary<Type, string[]> _nullables = new();
    public void Combo<T>(string title, EditorUndoBuffer buffer, FieldAccessor<T?> accessor) where T : struct, Enum
    {
        T? FromInt(int r)
        {
            if (r == 0) return null;
            var x = r - 1;
            return Unsafe.As<int, T>(ref x);
        }

        int FromT(T? value)
        {
            if (value == null) return 0;
            var v = value.Value;
            return Unsafe.As<T, int>(ref v) + 1;
        }

        if (!_nullables.TryGetValue(typeof(T), out var values))
        {
            values = Enum.GetNames<T>().Prepend("(none)").ToArray();
            _nullables[typeof(T)] = values;
        }

        Combo(title,
            FromT(accessor()),
            x => buffer.Set(title, accessor, FromInt(x)), values);
    }


    private static readonly Dictionary<Type, string[]> _enums = new();
    public void Combo<T>(string title, EditorUndoBuffer buffer, FieldAccessor<T> accessor) where T : struct, Enum
    {
        if (!_enums.TryGetValue(typeof(T), out var values)) {
            values = Enum.GetNames<T>();
            _enums[typeof(T)] = values;
        }
        Combo(title,
            Unsafe.As<T, int>(ref accessor()),
            x => buffer.Set(title, accessor, Unsafe.As<int, T>(ref x)), values);
    }

    void UpdateSuspendState()
    {
        if (!string.IsNullOrWhiteSpace(setTooltip))
        {
            suspend.FlagSuspend();
            return;
        }

        for (int i = 0; i < comboIndex; i++)
        {
            if (combos[i].Open)
            {
                suspend.FlagSuspend();
                return;
            }
        }

        for (int i = 0; i < strComboIndex; i++)
        {
            if (strCombos[i].Open)
            {
                suspend.FlagSuspend();
                return;
            }
        }
    }

    public void End()
    {
        // Skip processing this if not needed
        // Suspend()/Resume() can be expensive added up
        UpdateSuspendState();
        if (!suspend.ShouldSuspend())
        {
            return;
        }
        NodeEditor.Suspend();

        if(!string.IsNullOrWhiteSpace(setTooltip))
            ImGui.SetTooltip(setTooltip);

        ImGui.PushID((int)CurrentId);

        for (var i = 0; i < comboIndex; i++)
        {
            var c = combos[i];
            combos[i] = default;
            if(c.Open)
                ImGui.OpenPopup(c.Id);
            if (!ImGui.BeginPopup(c.Id, ImGuiWindowFlags.Popup))
                continue;
            suspend.FlagSuspend();
            for (var j = 0; j < c.Values.Length; j++)
            {
                ImGui.PushID(j);
                if (ImGui.MenuItem(c.Values[j]))
                    c.Set(j);
                ImGui.PopID();
            }
            ImGui.EndPopup();
        }

        for (var i = 0; i < strComboIndex; i++)
        {
            var c = strCombos[i];
            strCombos[i] = default;
            if (c.Open)
                ImGui.OpenPopup(c.Id);
            if (!ImGui.BeginPopup(c.Id, ImGuiWindowFlags.Popup))
                continue;
            suspend.FlagSuspend();
            if (c.AllowEmpty && ImGui.MenuItem("(none)##Empty"))
            {
                c.Set("");
                ImGui.CloseCurrentPopup();
            }

            var j = 0;
            foreach(var v in c.Values)
            {
                ImGui.PushID(j++);
                if (ImGui.MenuItem(v))
                {
                    c.Set(v);
                    ImGui.CloseCurrentPopup();
                }
                ImGui.PopID();
            }
            ImGui.EndPopup();
        }

        ImGui.PopID();
        ArrayPool<ComboData>.Shared.Return(combos);
        ArrayPool<StringComboData>.Shared.Return(strCombos);
        NodeEditor.Resume();
    }
}
