using ImGuiNET;
using LibreLancer.ImUI;
using System;
using System.IO;

namespace LancerEdit.Tools.BulkAudio;

public static class TrimEditorModal
{
    public static bool BeginTrimEditor(UiState uiState)
    {
        bool open = uiState.TrimEditingEntry != null;

        if (!ImGui.BeginPopupModal("trim_editor", ref open, ImGuiWindowFlags.AlwaysAutoResize))
            return false;

        // user closed the popup manually via the [X]
        if (!open)
            uiState.TrimEditingEntry = null;

        return true;
    }

    public static void Draw(UiState uiState)
    {
        var entry = uiState.TrimEditingEntry;
        var fileName = Path.GetFileName(entry.OriginalPath);

        ImGui.Text($"Trim MP3: {fileName}");
        ImGui.Separator();

        ImGui.Text("This MP3 has no trimming metadata.");
        ImGui.Text("Enter start/end samples manually (Audacity recommended).");
        ImGui.Spacing();

        int start = entry.TrimStart;
        if (ImGui.InputInt("Trim Start (samples)", ref start))
        {
            entry.TrimStart = Math.Max(0, start);
        }

        int end = entry.TrimEnd;
        if (ImGui.InputInt("Trim End (samples)", ref end))
        {
            entry.TrimEnd = Math.Max(0, end);
        }

        ImGui.Spacing();
        ImGui.Separator();

        if (ImGui.Button("OK", new System.Numerics.Vector2(120, 0)))
        {
            ImGui.CloseCurrentPopup();
            uiState.TrimEditingEntry = null;
        }

        ImGui.SameLine();

        if (ImGui.Button("Cancel", new System.Numerics.Vector2(120, 0)))
        {
            ImGui.CloseCurrentPopup();
            uiState.TrimEditingEntry = null;
        }
    }

    public static void End()
    {
        ImGui.EndPopup();
    }
}
