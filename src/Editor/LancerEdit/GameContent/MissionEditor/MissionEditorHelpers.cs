using System;
using System.Collections.Generic;
using ImGuiNET;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.MissionEditor;

public static class MissionEditorHelpers
{
    public static void AlertIfInvalidRef(Func<bool> refCheck)
    {
        if (refCheck())
        {
            return;
        }

        ImGui.SameLine();
        ImGui.Text(Icons.Warning.ToString());
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("This item has a broken reference. The game may be unstable or crash if used in this state.");
        }
    }

    public static void AddRemoveListButtons<T>(List<T> list) where T : new()
    {
        AddRemoveListButtonsInternal(list, () => new T());
    }

    public static void AddRemoveListButtons(List<string> list)
    {
        AddRemoveListButtonsInternal(list, () => string.Empty);
    }

    private static void AddRemoveListButtonsInternal<T>(IList<T> list, Func<T> defaultValue)
    {
        ImGui.PushID(list.Count);
        if (ImGui.Button(Icons.PlusCircle + "##Plus"))
        {
            list.Add(defaultValue());
            return;
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(list.Count is 0);
        if (ImGui.Button(Icons.X + "##Cross"))
        {
            list.RemoveAt(list.Count - 1);
        }

        ImGui.EndDisabled();

        ImGui.PopID();
    }
}
