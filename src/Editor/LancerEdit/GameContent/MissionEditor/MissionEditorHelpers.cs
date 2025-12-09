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

    public static void AddRemoveListButtons<T>(List<T> list, EditorUndoBuffer buffer) where T : new()
    {
        AddRemoveListButtonsInternal(list, buffer, () => new T());
    }

    public static void AddRemoveListButtons(List<string> list, EditorUndoBuffer buffer)
    {
        AddRemoveListButtonsInternal(list, buffer, () => string.Empty);
    }

    private static void AddRemoveListButtonsInternal<T>(List<T> list, EditorUndoBuffer buffer, Func<T> defaultValue)
    {
        ImGui.PushID(list.Count);
        if (ImGui.Button(Icons.PlusCircle + "##Plus"))
        {
            buffer.Commit(new ListAdd<T>("Item", list, defaultValue()));
            ImGui.PopID();
            return;
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(list.Count is 0);
        if (ImGui.Button(Icons.X + "##Cross"))
        {
            buffer.Commit(new ListRemove<T>("Item", list, list.Count - 1, list[^1]));
        }

        ImGui.EndDisabled();

        ImGui.PopID();
    }
}
