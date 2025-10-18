using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using ImGuiNET;

namespace LibreLancer.ImUI;

public class Toolbar : IDisposable
{
    private bool isOverflow = false;
    private bool isOverflowOpen = false;

    private Toolbar()
    {
    }

    public static Toolbar Begin(string id, bool sameLine)
    {
        ImGui.PushID(id);
        if (!sameLine) ImGui.Dummy(Vector2.Zero);
        return new Toolbar();
    }

    bool DoOverflow(string text, float margin)
    {
        if (isOverflow) return true;
        ImGui.SameLine();
        var textSize = ImGui.CalcTextSize(text);
        var cpos = ImGui.GetCursorPosX();
        var currentWidth = ImGui.GetWindowWidth();
        if (cpos + textSize.X + (margin * ImGuiHelper.Scale) > currentWidth) {
            isOverflow = true;
            if (ImGui.Button(">")) ImGui.OpenPopup("#overflow");
            isOverflowOpen = ImGui.BeginPopup("#overflow");
            return true;
        }
        return false;
    }

    public void DropdownButtonItem(string name, ref int selected, IReadOnlyList<DropdownOption> options)
    {
        string text = $"{options[selected].Icon}  {options[selected].Name}  ";
        if (DoOverflow(text, 15))
        {
            if (isOverflowOpen)
            {
                if (ImGui.BeginMenu(name))
                {
                    for (int i = 0; i < options.Count; i++)
                    {
                        if (ImGui.MenuItem(ImGuiExt.IDSafe($"{options[i].Icon} {options[i].Name}"), null, selected == i))
                        {
                            selected = i;
                        }
                    }
                    ImGui.EndMenu();
                }
            }
        }
        else
        {
            ImGuiExt.DropdownButton(name, ref selected, options);
        }
    }

    public bool ButtonItem(string name, bool enabled = true, string tooltip = null)
    {
        if (DoOverflow(name, 15))
        {
            if (isOverflowOpen)
            {
                var rv = ImGui.MenuItem(name, enabled);
                if(!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
                    ImGui.SetTooltip(tooltip);
                return rv;
            }

            return false;
        }
        var retval = ImGuiExt.Button(name, enabled);
        if(!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);
        return retval;
    }

    public void ToggleButtonItem(string name, ref bool isSelected)
    {
        if (DoOverflow(name, 15))
        {
            if (isOverflowOpen) ImGui.MenuItem(name, "", ref isSelected);
        }
        else
        {
            if (ImGuiExt.ToggleButton(name, isSelected)) isSelected = !isSelected;
        }
    }

    public void CheckItem(string name, ref bool isSelected)
    {
        if (DoOverflow(name, 50))
        {
            if (isOverflowOpen) ImGui.MenuItem(name, "", ref isSelected);
        }
        else
        {
            ImGui.Checkbox(name, ref isSelected);
        }
    }

    public void FloatSliderItem(string text, ref float value, float min, float max, string format)
    {
        if (DoOverflow(text, 250))
        {
            if (isOverflowOpen)
            {
                ImGui.MenuItem($"{text} {value}");
            }
        }
        else
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(text);
            ImGui.SameLine();
            ImGui.PushItemWidth(230);
            ImGui.SliderFloat($"##{text}", ref value, min, max, format);
            ImGui.PopItemWidth();
        }
    }
    public void TextItem(string text)
    {
        if (DoOverflow(text, 2))
        {
            if (isOverflowOpen) ImGui.MenuItem(text, false);
        }
        else
        {
            ImGui.Text(text);
        }
    }

    public void Dispose()
    {
        if (isOverflow && isOverflowOpen)
        {
            ImGui.EndPopup();
        }
        ImGui.PopID();
    }
}
