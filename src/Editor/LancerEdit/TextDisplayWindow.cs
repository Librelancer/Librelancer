using System;
using System.IO;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;

namespace LancerEdit;

public class TextDisplayWindow
{
    private static int unique = 0;
    public string Text;
    public string SuggestedFilename;

    private string title;
    public TextDisplayWindow(string text, string filename)
    {
        SuggestedFilename = filename;
        title = $"Text##{unique++}";
        this.Text = text;
    }

    FileDialogFilters TextFilter = new FileDialogFilters(
        new FileFilter("Text Files",".txt")
    );

    private bool isOpen = true;
    public bool Draw()
    {
        if (!isOpen) return false;
        ImGui.SetNextWindowSize(new Vector2(500,400), ImGuiCond.Once);
        ImGui.Begin(title, ref isOpen);
        ImGui.Text(SuggestedFilename);
        if (ImGui.Button("Save"))
        {
            FileDialog.Save(x => File.WriteAllText(x, Text), TextFilter);
        }
        ImGui.PushItemWidth(-1);
        ImGui.PushFont(ImGuiHelper.SystemMonospace);
        var height = ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - 10 * ImGuiHelper.Scale;
        ImGui.InputTextMultiline("##text", ref Text, UInt32.MaxValue, new Vector2(0,height), ImGuiInputTextFlags.ReadOnly);
        ImGui.PopFont();
        ImGui.PopItemWidth();
        ImGui.End();
        return isOpen;
    }
}
