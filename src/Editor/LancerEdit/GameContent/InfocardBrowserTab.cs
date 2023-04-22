// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.ImUI;
using LibreLancer.Infocards;

namespace LancerEdit;

public class InfocardBrowserTab : GameContentTab
{
    private const int MAX_PREV_LEN = 50;

    private readonly Infocard blankInfocard = new()
    {
        Nodes = new List<RichTextNode>(new[] {new RichTextTextNode {FontName = "Arial", FontSize = 12, Contents = ""}})
    };

    private int currentInfocard = -1;
    private int currentString = -1;
    private string currentXml;
    private volatile int dialogState;
    private readonly InfocardControl display;
    private bool doOpenSearch;

    private bool doOpenXml;
    private readonly FontManager fonts;

    private int gotoItem = -1;
    private int id;
    private readonly ListClipper infocardClipper;
    private readonly int[] infocardsIds;
    private bool isSearchInfocards;
    private readonly InfocardManager manager;
    private string resultTitle;
    private readonly TextBuffer searchBuffer = new();
    private bool searchCaseSensitive;
    private bool searchDlgOpen;
    private int[] searchResults;
    private bool searchResultsOpen;
    private string[] searchStringPreviews;
    private string[] searchStrings;
    private bool searchWholeWord;

    private bool showStrings = true;

    private readonly ListClipper stringClipper;
    private readonly int[] stringsIds;

    private TextBuffer txt;

    private readonly MainWindow win;
    private bool xmlDlgOpen;

    public InfocardBrowserTab(GameDataContext gameData, MainWindow win)
    {
        this.win = win;
        fonts = gameData.Fonts;
        manager = gameData.GameData.Ini.Infocards;
        stringsIds = manager.StringIds.ToArray();
        infocardsIds = manager.InfocardIds.ToArray();
        txt = new TextBuffer(8192);
        stringClipper = new ListClipper();
        infocardClipper = new ListClipper();
        Title = "Infocard Browser";
        display = new InfocardControl(win, blankInfocard, 100);
    }

    private void GotoString()
    {
        for (var i = 0; i < stringsIds.Length; i++)
            if (id == stringsIds[i])
            {
                gotoItem = i;
                currentString = i;
                DisplayInfoString();
            }
    }

    private void GotoInfocard()
    {
        for (var i = 0; i < infocardsIds.Length; i++)
            if (id == infocardsIds[i])
            {
                gotoItem = i;
                currentInfocard = i;
                currentXml = manager.GetXmlResource(infocardsIds[currentInfocard]);
                DisplayInfoXml();
            }
    }

    private void DisplayInfoXml()
    {
        if (currentInfocard == -1)
        {
            display.SetInfocard(blankInfocard);
            return;
        }

        display.SetInfocard(RDLParse.Parse(manager.GetXmlResource(infocardsIds[currentInfocard]), fonts));
    }

    private void DisplayInfoString()
    {
        if (currentString == -1)
        {
            display.SetInfocard(blankInfocard);
            return;
        }

        var str = manager.GetStringResource(stringsIds[currentString]);
        if (string.IsNullOrWhiteSpace(str))
        {
            display.SetInfocard(blankInfocard);
            return;
        }

        var infocard = new Infocard
        {
            Nodes = new List<RichTextNode>()
        };
        foreach (var ln in str.Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(ln))
                infocard.Nodes.Add(new RichTextTextNode
                {
                    Contents = ln, FontName = "Arial", FontSize = 22
                });
            infocard.Nodes.Add(new RichTextParagraphNode());
        }

        display.SetInfocard(infocard);
    }

    public override void Draw()
    {
        SearchDialog();
        InfocardXmlDialog();
        //strings vs infocards
        if (ImGuiExt.ToggleButton("Strings", showStrings))
        {
            showStrings = true;
            DisplayInfoString();
        }

        ImGui.SameLine();
        if (ImGuiExt.ToggleButton("Infocards", !showStrings))
        {
            showStrings = false;
            DisplayInfoXml();
        }

        ImGui.SameLine();
        ImGui.PushItemWidth(140);
        ImGui.InputInt("##id", ref id, 0, 0);
        ImGui.PopItemWidth();
        ImGui.SameLine();
        if (ImGui.Button("Go"))
        {
            if (showStrings)
                GotoString();
            else
                GotoInfocard();
        }

        ImGui.SameLine();
        if (ImGui.Button("Search..."))
        {
            if (showStrings) SearchStrings();
            else SearchInfocards();
        }

        ImGui.Separator();
        ImGui.Columns(2, "cols", true);
        //list
        ImGui.BeginChild("##list");
        if (showStrings)
        {
            if (gotoItem == -1)
            {
                stringClipper.Begin(stringsIds.Length);
                while (stringClipper.Step())
                    for (var i = stringClipper.DisplayStart; i < stringClipper.DisplayEnd; i++)
                        if (ImGui.Selectable(stringsIds[i] + "##" + i, currentString == i))
                        {
                            currentString = i;
                            DisplayInfoString();
                        }

                stringClipper.End();
            }
            else
            {
                for (var i = 0; i < stringsIds.Length; i++)
                {
                    ImGui.Selectable(stringsIds[i] + "##" + i, currentString == i);
                    if (currentString == i) ImGui.SetScrollHereY();
                }

                gotoItem = -1;
            }
        }
        else
        {
            if (gotoItem == -1)
            {
                infocardClipper.Begin(infocardsIds.Length);
                while (infocardClipper.Step())
                    for (var i = infocardClipper.DisplayStart; i < infocardClipper.DisplayEnd; i++)
                        if (ImGui.Selectable(infocardsIds[i] + "##" + i, currentInfocard == i))
                        {
                            currentInfocard = i;
                            currentXml = manager.GetXmlResource(infocardsIds[currentInfocard]);
                            DisplayInfoXml();
                        }

                infocardClipper.End();
            }
            else
            {
                for (var i = 0; i < infocardsIds.Length; i++)
                {
                    ImGui.Selectable(infocardsIds[i] + "##" + i, currentInfocard == i);
                    if (currentInfocard == i) ImGui.SetScrollHereY();
                }

                gotoItem = -1;
            }
        }

        ImGui.EndChild();
        ImGui.NextColumn();
        //Display
        if (showStrings)
        {
            if (currentString != -1)
            {
                ImGui.TextUnformatted(stringsIds[currentString].ToString());
                ImGui.SameLine();
                if (ImGui.Button("Copy Text"))
                    win.SetClipboardText(manager.GetStringResource(stringsIds[currentString]));
                ImGui.BeginChild("##display");
                display.Draw(ImGui.GetWindowWidth() - 15);
                ImGui.EndChild();
            }
        }
        else
        {
            if (currentInfocard != -1)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(infocardsIds[currentInfocard].ToString());
                ImGui.SameLine();
                if (ImGui.Button("View Xml")) doOpenXml = true;
                ImGui.SameLine();
                if (ImGui.Button("Copy Text")) win.SetClipboardText(display.InfocardText);
                ImGui.BeginChild("##display");
                display.Draw(ImGui.GetWindowWidth() - 15);
                ImGui.EndChild();
            }
        }
    }

    private void SearchStrings()
    {
        dialogState = 0;
        isSearchInfocards = false;
        searchBuffer.Clear();
        doOpenSearch = true;
    }

    private void SearchInfocards()
    {
        dialogState = 0;
        isSearchInfocards = true;
        searchBuffer.Clear();
        doOpenSearch = true;
    }

    private string EllipseIfNeeded(string s)
    {
        if (s.Length > MAX_PREV_LEN) s = s.Substring(0, MAX_PREV_LEN) + "...";
        return s.Replace("%", "%%");
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
                id = searchResults[i];
                if (isSearchInfocards) GotoInfocard();
                else GotoString();
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

    private void InfocardXmlDialog()
    {
        if (doOpenXml)
        {
            ImGui.OpenPopup(ImGuiExt.IDWithExtra("Xml", Unique));
            doOpenXml = false;
            xmlDlgOpen = true;
        }

        if (ImGui.BeginPopupModal(ImGuiExt.IDWithExtra("Xml", Unique), ref xmlDlgOpen,
                ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (ImGui.Button("Copy To Clipboard")) win.SetClipboardText(currentXml);

            ImGui.PushFont(ImGuiHelper.SystemMonospace);
            ImGui.InputTextMultiline("##xml", ref currentXml, uint.MaxValue, new Vector2(400),
                ImGuiInputTextFlags.ReadOnly);
            ImGui.PopFont();
            ImGui.EndPopup();
        }
    }

    private void SearchDialog()
    {
        if (doOpenSearch)
        {
            ImGui.OpenPopup(ImGuiExt.IDWithExtra("Search", Unique));
            doOpenSearch = false;
            searchDlgOpen = true;
            searchResultsOpen = false;
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
        searchBuffer.InputText("##searchtext", ImGuiInputTextFlags.None, 200);
        ImGui.Checkbox("Case Sensitive", ref searchCaseSensitive);
        ImGui.Checkbox("Match Whole World", ref searchWholeWord);
        if (ImGui.Button("Go"))
        {
            var str = searchBuffer.GetText();
            if (!string.IsNullOrWhiteSpace(str))
            {
                resultTitle = ImGuiExt.IDSafe($"Results for '{str}'");
                dialogState = 1;
                Regex r;
                var regOptions = searchCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                if (searchWholeWord)
                    r = new Regex($"\\b{Regex.Escape(str)}\\b", regOptions);
                else
                    r = new Regex(Regex.Escape(str), regOptions);

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

    public override void Dispose()
    {
        stringClipper.Dispose();
        infocardClipper.Dispose();
    }
}