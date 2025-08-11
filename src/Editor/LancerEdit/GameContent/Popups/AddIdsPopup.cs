// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.ImUI;
using ResourceType = LibreLancer.ContentEdit.EditableInfocardManager.ResourceType;

namespace LancerEdit.GameContent.Popups;

public class AddIdsPopup : PopupWindow
{
    public override string Title { get; set; }

    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;
    
    private readonly EditableInfocardManager manager;
    private readonly MainWindow win;
    private readonly bool isInfocard;
    private readonly Action<int> onIdCreated;
    private readonly bool autoSave;
    private int newIds;
    private string newContent = "";

    public AddIdsPopup(EditableInfocardManager manager, MainWindow win, bool isInfocard, Action<int> onIdCreated = null, bool autoSave = false)
    {
        this.manager = manager;
        this.win = win;
        this.isInfocard = isInfocard;
        this.onIdCreated = onIdCreated;
        this.autoSave = autoSave;
        Title = isInfocard ? "Add Infocard" : "Add String";
        newContent = isInfocard ?
            "<RDL>\n  <PUSH/>\n  <TEXT>New Infocard</TEXT>\n</RDL>" :
            "New String";
    }

    public override void Draw(bool appearing)
    {
        // Title
        ImGui.Text("Assign New ID");
        
        // Warning for auto-save mode
        if (autoSave)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.8f, 0.2f, 1.0f)); // Orange warning color
            ImGui.TextWrapped("All IDS changes will be saved upon creating this ID");
            ImGui.PopStyleColor();
        }
        
        ImGui.Separator();

        // Input
        ImGui.AlignTextToFramePadding();
        ImGui.Text("New ID:");
        ImGui.SameLine();
        ImGui.PushItemWidth(130 * ImGuiHelper.Scale);

        bool idError = (newIds <= 0 ||
                        newIds > manager.MaxIds ||
                        manager.StringExists(newIds) ||
                        manager.XmlExists(newIds));

        if (idError)
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.3f, 0f, 0f, 1f));

        ImGui.InputInt("##newids", ref newIds, 0, 0);

        if (idError)
            ImGui.PopStyleColor();

        ImGui.PopItemWidth();

        // Find Next button
        ImGui.SameLine();
        if (ImGui.Button("Find Next"))
        {
            var r = manager.NextFreeId(newIds);
            if (r == -1)
                win.ErrorDialog("No ID available. Add a new .dll file.");
            else
                newIds = r;
        }

        // Find Highest Free button
        ImGui.SameLine();
        if (ImGui.Button("Find Highest Free"))
        {
            var r = manager.HighestFreeId(isInfocard ?
                ResourceType.Infocard : ResourceType.String);
            if (r == -1)
                win.ErrorDialog("No free ID found.");
            else
                newIds = r;
        }

        // Content input
        ImGui.Spacing();
        ImGui.Text("Content:");
        if (isInfocard)
        {
            Vector2 size = new Vector2(
                ImGui.GetContentRegionAvail().X,
                10 * ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2
            );
            ImGui.InputTextMultiline("##content", ref newContent, 4096, size);
        }
        else
        {
            ImGui.InputTextMultiline("##content", ref newContent, 255,
                new Vector2(ImGui.GetContentRegionAvail().X, 2 * ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2));
        }

        // Validation and information
        ImGui.Spacing();
        ImGui.BeginGroup();
        {
            if (newIds <= 0)
                ImGui.TextColored(Color4.Red, "ID must be 1 or higher.");
            else if (newIds > manager.MaxIds)
                ImGui.TextColored(Color4.Red, $"{newIds} exceeds max ID {manager.MaxIds}. Add a new .dll file.");
            else if (manager.StringExists(newIds))
                ImGui.TextColored(Color4.Red, "ID already in use (string).");
            else if (manager.XmlExists(newIds))
                ImGui.TextColored(Color4.Red, "ID already in use (infocard).");
            else
            {
                int dllIndex = newIds >> 16;
                string dllFile = Path.GetFileName(manager.Dlls[dllIndex].SavePath);
                ImGui.TextColored(Color4.Green, $"ID is available");
                ImGui.TextDisabled($"DLL: {dllFile} (IDs {dllIndex*65536}-{dllIndex*65536+65535})");
            }
        }
        ImGui.EndGroup();

        // Actions
        ImGui.Separator();
        bool canSave = !idError;

        ImGui.BeginGroup();
        {
            if (ImGuiExt.Button("Save", canSave))
            {
                if (isInfocard)
                {
                    manager.SetXmlResource(newIds, newContent);
                }
                else
                {
                    manager.SetStringResource(newIds, newContent);
                }
                
                // Auto-save if requested (when called from search)
                if (autoSave)
                {
                    manager.Save();
                }
                
                // Call the callback if provided
                onIdCreated?.Invoke(newIds);
                
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
        }
        ImGui.EndGroup();
    }
}