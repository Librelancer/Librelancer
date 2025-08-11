using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.ImUI;
using LibreLancer.Infocards;

namespace LancerEdit.GameContent.Popups;

public class IdsSearch : PopupWindow
{
    public override string Title { get; set; }

    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    public override bool NoClose => dialogState == 1 || shouldClose;


    private const int MAX_PREV_LEN = 50;

    private bool searchCaseSensitive;
    private bool searchDlgOpen;
    private int[] searchResults;
    private bool searchResultsOpen;
    private string[] searchStringPreviews;
    private string[] searchStrings;
    private bool searchWholeWord;
    private volatile int dialogState;
    private bool shouldClose = false;

    private string searchText = "";
    private bool isSearchInfocards = false;

    private static int _uid = 0;
    private int Unique = Interlocked.Increment(ref _uid);

    private string resultTitle;

    private Action<int> onSearchResult;

    private InfocardManager manager;
    private FontManager fonts;
    private MainWindow win;
    private PopupManager popups = new PopupManager();

    private IdsSearch(InfocardManager manager, FontManager fonts, MainWindow win)
    {
        this.manager = manager;
        this.fonts = fonts;
        this.win = win;
        Title = ImGuiExt.IDWithExtra("Search", Unique);
    }

    public static IdsSearch SearchStrings(InfocardManager manager, FontManager fonts, Action<int> onSelect)
    {
        var dlg = new IdsSearch(manager, fonts, null);
        dlg.dialogState = 0;
        dlg.isSearchInfocards = false;
        dlg.searchText = "";
        dlg.onSearchResult = onSelect;
        return dlg;
    }

    public static IdsSearch SearchStrings(InfocardManager manager, FontManager fonts, MainWindow win, Action<int> onSelect)
    {
        var dlg = new IdsSearch(manager, fonts, win);
        dlg.dialogState = 0;
        dlg.isSearchInfocards = false;
        dlg.searchText = "";
        dlg.onSearchResult = onSelect;
        return dlg;
    }

    public static IdsSearch SearchInfocards(InfocardManager manager, FontManager fonts, Action<int> onSelect)
    {
        var dlg = new IdsSearch(manager, fonts, null);
        dlg.dialogState = 0;
        dlg.isSearchInfocards = true;
        dlg.searchText = "";
        dlg.onSearchResult = onSelect;
        return dlg;
    }

    public static IdsSearch SearchInfocards(InfocardManager manager, FontManager fonts, MainWindow win, Action<int> onSelect)
    {
        var dlg = new IdsSearch(manager, fonts, win);
        dlg.dialogState = 0;
        dlg.isSearchInfocards = true;
        dlg.searchText = "";
        dlg.onSearchResult = onSelect;
        return dlg;
    }

    public override void Draw(bool appearing)
    {
        // Close the window if requested
        if (shouldClose)
        {
            ImGui.CloseCurrentPopup();
            return;
        }
        
        popups.Run();
        if (searchResultsOpen) DrawSearchResults();
        if (dialogState == 0)
        {
            SearchWindow();
        }
        else if (dialogState == 1)
        {
            SearchStatus();
        }
        else
        {
            DrawSearchResults();
        }
    }

    public record SearchResults(int[] Ids, string[] Values);

    public static Task<SearchResults> Search(
        InfocardManager infocards,
        string searchText,
        bool isStrings,
        bool ignoreCase,
        bool wholeWord)
        => Task.Run(() =>
        {
            Regex r;
            var regOptions = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            if (wholeWord)
                r = new Regex($"\\b{Regex.Escape(searchText)}\\b", regOptions);
            else
                r = new Regex(Regex.Escape(searchText), regOptions);
            var results = new List<int>();
            var resStrings = new List<string>();
            if (isStrings)
            {
                foreach (var kv in infocards.AllStrings)
                {
                    if (r.IsMatch(kv.Value))
                    {
                        results.Add(kv.Key);
                        resStrings.Add(kv.Value);
                    }
                }
            }
            else
            {
                foreach (var kv in infocards.AllXml)
                {
                    if (r.IsMatch(kv.Value))
                    {
                        results.Add(kv.Key);
                        resStrings.Add(kv.Value);
                    }
                }
            }
            return new SearchResults(results.ToArray(), resStrings.ToArray());
        });


    private void SearchWindow()
    {
        ImGui.Text(isSearchInfocards ? "Search Infocards" : "Search Strings");
        ImGui.InputText("##searchtext", ref searchText, 65536, ImGuiInputTextFlags.None);
        ImGui.Checkbox("Case Sensitive", ref searchCaseSensitive);
        ImGui.Checkbox("Match Whole Word", ref searchWholeWord);
        if (ImGui.Button("Go"))
        {
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                resultTitle = ImGuiExt.IDSafe($"Results for '{searchText}'");
                dialogState = 1;

                Search(manager, searchText, !isSearchInfocards, !searchCaseSensitive, searchWholeWord)
                    .ContinueWith(res =>
                    {
                        searchResults = res.Result.Ids;
                        searchStrings = res.Result.Values;
                        searchStringPreviews = new string[searchStrings.Length];
                        dialogState = 2;
                    });
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Create New ID"))
        {
            if (manager is LibreLancer.ContentEdit.EditableInfocardManager editableManager && win != null)
            {
                popups.OpenPopup(new AddIdsPopup(editableManager, win, isSearchInfocards, (newId) =>
                {
                    onSearchResult(newId);
                    shouldClose = true;
                }, autoSave: true));
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
    }

    private void SearchStatus()
    {
        ImGuiExt.Spinner("##spinner", 10, 2, ImGui.GetColorU32(ImGuiCol.ButtonHovered, 1));
        ImGui.SameLine();
        ImGui.Text("Searching");
    }

    private void DrawSearchResults()
    {
        ImGui.Text(resultTitle);
        ImGui.BeginChild("##results", new Vector2(200, 200), ImGuiChildFlags.Borders);
        for (var i = 0; i < searchResults.Length; i++)
        {
            if (ImGui.Selectable(searchResults[i].ToString()))
            {
                onSearchResult(searchResults[i]);
                ImGui.CloseCurrentPopup();
            }

            if (ImGui.IsItemHovered())
            {
                if (isSearchInfocards)
                {
                    if (searchStringPreviews[i] == null)
                        try
                        {
                            searchStringPreviews[i] =
                                EllipseIfNeeded(RDLParse.Parse(searchStrings[i], fonts).ExtractText());
                        }
                        catch (Exception)
                        {
                            searchStringPreviews[i] = EllipseIfNeeded(searchStrings[i]);
                        }

                    ImGui.SetTooltip(searchStringPreviews[i]);
                }
                else
                {
                    ImGui.SetTooltip(EllipseIfNeeded(searchStrings[i]));
                }
            }
        }

        ImGui.EndChild();
    }

    private string EllipseIfNeeded(string s)
    {
        if (s.Length > MAX_PREV_LEN) s = s.Substring(0, MAX_PREV_LEN) + "...";
        return s.Replace("%", "%%");
    }
}
