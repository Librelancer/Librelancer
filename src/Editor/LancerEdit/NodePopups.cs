using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;

namespace LancerEdit;

public struct NodePopups
{
    private string setTooltip;
    private int comboIndex;
    private ComboData[] combos;
    record struct ComboData(bool Open, Action<int> Set, string Id, string[] Values);

    private StringComboData[] strCombos;
    private int strComboIndex;
    record struct StringComboData(bool Open, Action<string> Set, string Id, IEnumerable<string> Values);

    public NodeId CurrentId;

    public static NodePopups Begin(NodeId id) => new()
    {
        CurrentId = id,
        combos = ArrayPool<ComboData>.Shared.Rent(16),
        strCombos = ArrayPool<StringComboData>.Shared.Rent(16),
    };

    public void Tooltip(string tooltip)
    {
        setTooltip = tooltip;
    }

    public void Combo(string title, int selectedValue, Action<int> set, string[] values)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text(title);
        ImGui.SameLine();
        combos[comboIndex++] = new ComboData(ImGuiExt.ComboButton(title, values[selectedValue]), set, title, values);
    }

    public void StringCombo(string title, string selectedValue, Action<string> set, IEnumerable<string> values)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text(title);
        ImGui.SameLine();

        var enumerable = values as string[] ?? values.ToArray();
        strCombos[strComboIndex++] = new StringComboData(ImGuiExt.ComboButton(title, enumerable.FirstOrDefault(x => x.Equals(selectedValue, StringComparison.OrdinalIgnoreCase)) ?? selectedValue), set, title, enumerable);
    }


    private static readonly Dictionary<Type, string[]> _enums = new Dictionary<Type, string[]>();
    public void Combo<T>(string title, T selectedValue, Action<T> set) where T : struct, Enum
    {
        if (!_enums.TryGetValue(typeof(T), out var values)) {
            values = Enum.GetNames<T>();
            _enums[typeof(T)] = values;
        }
        Combo(title, Unsafe.As<T, int>(ref selectedValue), x => set(Unsafe.As<int, T>(ref x)), values);
    }

    public void End()
    {
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
            if(c.Open)
                ImGui.OpenPopup(c.Id);
            if (!ImGui.BeginPopup(c.Id, ImGuiWindowFlags.Popup))
            {
                continue;
            }

            var j = 0;
            foreach(var v in c.Values)
            {
                ImGui.PushID(j++);
                if (ImGui.MenuItem(v))
                    c.Set(v);
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
