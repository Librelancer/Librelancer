using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Data;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public sealed class StringSelection : PopupWindow
{
    public override string Title { get; set; } = "String";
    public override Vector2 InitSize { get; } = new Vector2(610, 400) * ImGuiHelper.Scale;

    private InfocardManager manager;
    private int current;

    private string searchText = "";
    private string resultText = "";
    private bool searchCaseSensitive;
    private bool searchWholeWord;
    private bool appearing = true;
    private bool shouldClose = false;

    private int[] currentResults;

    private string display;
    private MainWindow window;
    private Action<int> onSelected;
    private PopupManager popups = new PopupManager();

    Dictionary<int, string> previews = new Dictionary<int, string>();

    string GetXmlPreview(int ids)
    {
        if (!previews.TryGetValue(ids, out var prev))
        {
            var txt = manager.GetStringResource(ids);
            if (txt.Length > 100)
                prev = txt.Substring(0, 100) + "...";
            else
                prev = txt;
            previews.Add(ids, prev);
        }
        return prev;
    }



    public StringSelection(int selected, InfocardManager manager,Action<int> onSelected)
    {
        current = selected;
        this.manager = manager;
        this.onSelected = onSelected;
        currentResults = manager.AllStrings.Select(x => x.Key).Order().ToArray();
        RefreshString();
    }

    void RefreshString()
    {
        display = null;
        if (current == 0)
            return;
        display = manager.GetStringResource(current);
    }

    private bool searching = false;

    public override void Draw(bool appearing)
    {
        // Close the window if requested
        if (shouldClose)
        {
            ImGui.CloseCurrentPopup();
            return;
        }

        popups.Run();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Search: ");
        ImGui.SameLine();
        ImGui.PushItemWidth(-1);
        ImGui.InputText("##searchtext", ref searchText, 65536, ImGuiInputTextFlags.None);
        ImGui.PopItemWidth();
        ImGui.Checkbox("Case Sensitive", ref searchCaseSensitive);
        ImGui.SameLine();
        ImGui.Checkbox("Match Whole World", ref searchWholeWord);
        ImGui.SameLine();
        if (ImGui.Button("Go"))
        {
            if (string.IsNullOrEmpty(searchText))
            {
                resultText = "";
                currentResults = manager.AllStrings.Select(x => x.Key).Order().ToArray();
            }
            else if (int.TryParse(searchText.Trim(), out var newId) &&
                     manager.HasStringResource(newId))
            {
                appearing = true;
                current = newId;
                resultText = "";
                RefreshString();
            }
            else
            {
                searching = true;
                IdsSearch.Search(manager, searchText, true, !searchCaseSensitive, !searchWholeWord)
                    .ContinueWith(res =>
                    {
                        resultText = $"Results for '{searchText}";
                        currentResults = res.Result.Ids;
                        searching = false;
                    });
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("Create New ID"))
        {
            if (manager is LibreLancer.ContentEdit.EditableInfocardManager editableManager)
            {
                popups.OpenPopup(new AddIdsPopup(editableManager, window, false, (newId) =>
                {
                    current = newId;
                    RefreshString();
                    // Refresh the results to include the new ID
                    currentResults = manager.AllStrings.Select(x => x.Key).Order().ToArray();
                    onSelected(newId);
                    shouldClose = true;
                }, autoSave: true));
            }
        }
        if (!string.IsNullOrWhiteSpace(resultText)) {
            ImGui.Text(resultText);
        }

        if (searching) {
            ImGuiExt.Spinner("##spinner", 10, 2, ImGui.GetColorU32(ImGuiCol.ButtonHovered, 1));
            ImGui.SameLine();
            ImGui.Text("Searching");
            return;
        }

        var tableHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing();
        if (!ImGui.BeginTable("##main", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.NoHostExtendY, new Vector2(-1, tableHeight)))
            return;
        ImGui.TableSetupColumn("Strings", ImGuiTableColumnFlags.WidthFixed, 205 * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("Preview");
        ImGui.TableHeadersRow();
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.BeginChild("##items");
        for (int i = 0; i < currentResults.Length; i++)
        {
            if (ImGui.Selectable(currentResults[i].ToString(), current == currentResults[i]))
            {
                current = currentResults[i];
                RefreshString();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetNextWindowSize(new Vector2(300, 0), ImGuiCond.Always);
                if (ImGui.BeginTooltip())
                {
                    ImGui.TextWrapped(GetXmlPreview(currentResults[i]));
                    ImGui.EndTooltip();
                }
            }
            if (appearing && currentResults[i] == current)
                ImGui.SetScrollHereY();
        }
        appearing = false;
        ImGui.EndChild();
        ImGui.TableNextColumn();
        if (display != null)
        {
            ImGui.BeginChild("##display");
            ImGui.TextWrapped(display);
            ImGui.EndChild();
        }
        ImGui.EndTable();
        if (ImGui.Button("Ok")) {
            onSelected(current);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGuiExt.Button("Clear", current != 0)) {
            onSelected(0);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel")) {
            ImGui.CloseCurrentPopup();
        }
    }
}
