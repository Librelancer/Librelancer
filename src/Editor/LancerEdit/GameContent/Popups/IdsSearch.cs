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

    public override bool NoClose => dialogState == 1;


    private const int MAX_PREV_LEN = 50;

    private bool searchCaseSensitive;
    private bool searchDlgOpen;
    private int[] searchResults;
    private bool searchResultsOpen;
    private string[] searchStringPreviews;
    private string[] searchStrings;
    private bool searchWholeWord;
    private volatile int dialogState;

    private string searchText = "";
    private bool isSearchInfocards = false;

    private static int _uid = 0;
    private int Unique = Interlocked.Increment(ref _uid);

    private string resultTitle;

    private Action<int> onSearchResult;

    private InfocardManager manager;
    private FontManager fonts;

    private IdsSearch(InfocardManager manager, FontManager fonts)
    {
        this.manager = manager;
        this.fonts = fonts;
        Title = ImGuiExt.IDWithExtra("Search", Unique);
    }

    public static IdsSearch SearchStrings(InfocardManager manager, FontManager fonts, Action<int> onSelect)
    {
        var dlg = new IdsSearch(manager, fonts);
        dlg.dialogState = 0;
        dlg.isSearchInfocards = false;
        dlg.searchText = "";
        dlg.onSearchResult = onSelect;
        return dlg;
    }

    public static IdsSearch SearchInfocards(InfocardManager manager, FontManager fonts, Action<int> onSelect)
    {
        var dlg = new IdsSearch(manager, fonts);
        dlg.dialogState = 0;
        dlg.isSearchInfocards = true;
        dlg.searchText = "";
        dlg.onSearchResult = onSelect;
        return dlg;
    }

    public override void Draw()
    {
        if (searchResultsOpen) SearchResults();
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
            SearchResults();
        }
    }

    private void SearchWindow()
    {
        ImGui.Text(isSearchInfocards ? "Search Infocards" : "Search Strings");
        ImGui.InputText("##searchtext", ref searchText, 65536, ImGuiInputTextFlags.None);
        ImGui.Checkbox("Case Sensitive", ref searchCaseSensitive);
        ImGui.Checkbox("Match Whole World", ref searchWholeWord);
        if (ImGui.Button("Go"))
        {
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                resultTitle = ImGuiExt.IDSafe($"Results for '{searchText}'");
                dialogState = 1;
                Regex r;
                var regOptions = searchCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                if (searchWholeWord)
                    r = new Regex($"\\b{Regex.Escape(searchText)}\\b", regOptions);
                else
                    r = new Regex(Regex.Escape(searchText), regOptions);

                if (isSearchInfocards)
                    Task.Run(() =>
                    {
                        var results = new List<int>();
                        var resStrings = new List<string>();
                        foreach (var kv in manager.AllXml)
                            if (r.IsMatch(kv.Value))
                            {
                                results.Add(kv.Key);
                                resStrings.Add(kv.Value);
                            }

                        searchResults = results.ToArray();
                        searchStrings = resStrings.ToArray();
                        searchStringPreviews = new string[searchStrings.Length];
                        dialogState = 2;
                    });
                else
                    Task.Run(() =>
                    {
                        var results = new List<int>();
                        var resStrings = new List<string>();
                        foreach (var kv in manager.AllStrings)
                            if (r.IsMatch(kv.Value))
                            {
                                results.Add(kv.Key);
                                resStrings.Add(kv.Value);
                            }

                        searchResults = results.ToArray();
                        searchStrings = resStrings.ToArray();
                        dialogState = 2;
                    });
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

    private void SearchResults()
    {
        ImGui.TextUnformatted(resultTitle);
        ImGui.BeginChild("##results", new Vector2(200, 200), ImGuiChildFlags.Border);
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
