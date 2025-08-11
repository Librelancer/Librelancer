using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.ImUI;
using LibreLancer.Infocards;

namespace LancerEdit.GameContent.Popups;

public sealed class InfocardSelection : PopupWindow
{
    public override string Title { get; set; } = "Infocard";
    public override Vector2 InitSize { get; } = new Vector2(610, 400) * ImGuiHelper.Scale;

    private InfocardManager manager;
    private FontManager fonts;
    private int current;

    private string searchText = "";
    private string resultText = "";
    private bool searchCaseSensitive;
    private bool searchWholeWord;
    private bool appearing = true;
    private bool shouldClose = false;

    private int[] currentResults;

    private InfocardControl display;
    private MainWindow window;
    private Action<int> onSelected;
    private PopupManager popups = new PopupManager();

    Dictionary<int, string> previews = new Dictionary<int, string>();

    string GetXmlPreview(int ids)
    {
        if (!previews.TryGetValue(ids, out var prev))
        {
            var txt = RDLParse.Parse(manager.GetXmlResource(ids), fonts).ExtractText();
            if (txt.Length > 100)
                prev = txt.Substring(0, 100) + "...";
            else
                prev = txt;
            previews.Add(ids, prev);
        }
        return prev;
    }



    public InfocardSelection(int selected, MainWindow win, InfocardManager manager, FontManager fonts, Action<int> onSelected)
    {
        current = selected;
        this.manager = manager;
        this.fonts = fonts;
        this.window = win;
        this.onSelected = onSelected;
        currentResults = manager.AllXml.Select(x => x.Key).Order().ToArray();
        LoadInfocard();
    }

    void LoadInfocard()
    {
        display?.Dispose();
        display = null;
        if (current == 0)
            return;
        var txt = manager.GetXmlResource(current);
        if (string.IsNullOrWhiteSpace(txt))
            return;
        var icard = RDLParse.Parse(txt, fonts);
        display = new InfocardControl(window, icard, 395 * ImGuiHelper.Scale);
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
                currentResults = manager.AllXml.Select(x => x.Key).Order().ToArray();
            }
            else
            {
                searching = true;
                IdsSearch.Search(manager, searchText, false, !searchCaseSensitive, !searchWholeWord)
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
                popups.OpenPopup(new AddIdsPopup(editableManager, window, true, (newId) =>
                {
                    current = newId;
                    LoadInfocard();
                    // Refresh the results to include the new ID
                    currentResults = manager.AllXml.Select(x => x.Key).Order().ToArray();
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
        ImGui.TableSetupColumn("Infocards", ImGuiTableColumnFlags.WidthFixed, 205 * ImGuiHelper.Scale);
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
                LoadInfocard();
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
            display.Draw(ImGui.GetContentRegionAvail().X);
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

    public override void OnClosed()
    {
        display?.Dispose();
    }
}
