using System;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.IO;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class FileSelectionPopup : PopupWindow
{
    public override string Title { get; set; }
    public override Vector2 InitSize => new Vector2(300, 400) * ImGuiHelper.Scale;
    
    private string[] files;
    private string[] displayNames;
    private Action<string> onSelect;
    private string currentFile;
    private int selectedIndex = -1;
    private string basePath;
    private FileSystem vfs;
    private string filterExtension;

    public FileSelectionPopup(string title, FileSystem vfs, string basePath, string filterExtension,
        string currentFile, Action<string> onSelect)
    {
        Title = title;
        this.vfs = vfs;
        this.basePath = basePath;
        this.filterExtension = filterExtension;
        this.currentFile = currentFile;
        this.onSelect = onSelect;


        // Get files from VFS
        try
        {
            files = vfs.GetFiles(basePath)
                .Where(f => f.EndsWith(filterExtension, StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f)
                .ToArray();
            FLLog.Debug("FileSelectionPopup", $"Found {files.Length} files matching filter");
        }
        catch (Exception ex)
        {
            FLLog.Error("FileSelectionPopup", $"Exception getting files from path '{basePath}': {ex.Message}");
            files = Array.Empty<string>();
        }

        displayNames = files.Select(f => Path.GetFileName(f)).ToArray();

        // Find current selection
        if (!string.IsNullOrEmpty(currentFile))
        {
            var currentFileName = Path.GetFileName(currentFile);
            for (int i = 0; i < displayNames.Length; i++)
            {
                if (displayNames[i].Equals(currentFileName, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }
        }
    }

    public override void Draw(bool appearing)
    {
        // Draw file list
        if (files.Length == 0)
        {
            ImGui.Text("No files found");
            ImGui.Separator();
            if (ImGui.Button("Close"))
                ImGui.CloseCurrentPopup();
            return;
        }
        
        ImGui.BeginChild("##filelist", new Vector2(-1, -ImGuiHelper.Scale * 50));
        
        for (int i = 0; i < displayNames.Length; i++)
        {
            bool isSelected = (selectedIndex == i);
            if (ImGui.Selectable(displayNames[i], isSelected))
            {
                selectedIndex = i;
            }
            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
            {
                // Double click to select
                SelectFile(i);
            }
        }
        
        ImGui.EndChild();
        
        ImGui.Separator();
        
        // Buttons
        ImGui.BeginDisabled(selectedIndex < 0);
        if (ImGui.Button("Select"))
        {
            SelectFile(selectedIndex);
        }
        ImGui.EndDisabled();
        
        ImGui.SameLine();
        if (ImGui.Button("Close"))
            ImGui.CloseCurrentPopup();
    }
    
    private void SelectFile(int index)
    {
        if (index >= 0 && index < files.Length)
        {
            onSelect(files[index]);
            ImGui.CloseCurrentPopup();
        }
    }
}
