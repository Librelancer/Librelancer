using System;
using System.Numerics;
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

    public bool ButtonItem(string name)
    {
        if (isOverflow)
        {
            if (isOverflowOpen)
                return ImGui.MenuItem(name);
            return false;
        }

        ImGui.SameLine();
        var textSize = ImGui.CalcTextSize(name);
        var cpos = ImGuiNative.igGetCursorPosX();
        var currentWidth = ImGui.GetWindowWidth();
        if (cpos + textSize.X + (15 * ImGuiHelper.Scale) > currentWidth)
        {
            isOverflow = true;
            if (ImGui.Button(">")) ImGui.OpenPopup("#overflow");
            isOverflowOpen = ImGui.BeginPopup("#overflow");
            if (isOverflowOpen)
                return ImGui.MenuItem(name);
            return false;
        }
        else
        {
            return ImGui.Button(name);
        }
    }


    public void CheckItem(string name, ref bool isSelected)
    {
        if (isOverflow)
        {
            if (isOverflowOpen) ImGui.MenuItem(name, "", ref isSelected);
            return;
        }

        ImGui.SameLine();
        var textSize = ImGui.CalcTextSize(name);
        var cpos = ImGuiNative.igGetCursorPosX();
        var currentWidth = ImGui.GetWindowWidth();
        if (cpos + textSize.X + (50 * ImGuiHelper.Scale) > currentWidth)
        {
            isOverflow = true;
            if (ImGui.Button(">")) ImGui.OpenPopup("#overflow");
            isOverflowOpen = ImGui.BeginPopup("#overflow");
            if (isOverflowOpen) ImGui.MenuItem(name, "", ref isSelected);
        }
        else
        {
            ImGui.Checkbox(name, ref isSelected);
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