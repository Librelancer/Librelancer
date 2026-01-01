using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;

namespace LancerEdit.GameContent.Lookups;

public abstract class ObjectLookup<T> where T : class
{
    private record Display(string Name, T Value);

    private Display sel;

    public T Selected => sel?.Value;
    public T Hovered => dropdown?.Hovered?.Value;
    public bool IsOpen => dropdown.IsOpen;

    private SearchDropdown<Display> dropdown;
    private Display[] options;

    public Action<T> OnSelected;

    protected void CreateDropdown(string id, IEnumerable<T> values, Func<T,string> name, T initial)
    {
        options = values
            .Select(x => new Display( name(x), x))
            .ToArray();
        if(initial != null)
            sel = options.FirstOrDefault(x => x.Value == initial);
        dropdown = new SearchDropdown<Display>(id,
             x => x?.Name ?? "(none)",
            x =>
            {
                sel = x;
                OnSelected?.Invoke(x.Value);
            },
            sel, options);
    }

    public void SetSelected(T value)
    {
        sel = options.FirstOrDefault(x => x.Value == value);
        dropdown.SetSelected(sel);
    }

    public void Draw(string label = null)
    {
        if (Controls.InEditorTable)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
        }
        if (label != null)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);
        }
        if (Controls.InEditorTable)
        {
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
        }
        dropdown.Draw();
    }
}
