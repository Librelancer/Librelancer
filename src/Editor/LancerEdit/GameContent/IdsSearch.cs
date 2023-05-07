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

namespace LancerEdit;

public class IdsSearch
{
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

    public IdsSearch(InfocardManager manager, FontManager fonts)
    {
        this.manager = manager;
        this.fonts = fonts;
    }
    
    public void SearchStrings(Action<int> onSelect)
    {
        dialogState = 0;
        isSearchInfocards = false;
        searchText = "";
        doOpenSearch = true;
        onSearchResult = onSelect;
    }

    public void SearchInfocards(Action<int> onSelect)
    {
        dialogState = 0;
        isSearchInfocards = true;
        searchText = "";
        doOpenSearch = true;
        onSearchResult = onSelect;
    }

    private bool doOpenSearch = false;
    
    public void Draw()
    {
        if (doOpenSearch)
        {
            doOpenSearch = false;
            searchDlgOpen = true;
            searchResultsOpen = false;
            ImGui.OpenPopup(ImGuiExt.IDWithExtra("Search", Unique));
        }
        if (searchResultsOpen) SearchResults();
        if (ImGui.BeginPopupModal(ImGuiExt.IDWithExtra("Search", Unique), ref searchDlgOpen,
                ImGuiWindowFlags.AlwaysAutoResize))
        {
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
                searchResultsOpen = true;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
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
        ImGui.Begin(ImGuiExt.IDWithExtra("Search", Unique), ref searchResultsOpen, ImGuiWindowFlags.AlwaysAutoResize);
        ImGui.TextUnformatted(resultTitle);
        ImGui.BeginChild("##results", new Vector2(200, 200), true);
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
        ImGui.End();
    }
    
    private string EllipseIfNeeded(string s)
    {
        if (s.Length > MAX_PREV_LEN) s = s.Substring(0, MAX_PREV_LEN) + "...";
        return s.Replace("%", "%%");
    }
    
}