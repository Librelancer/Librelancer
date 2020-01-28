// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LibreLancer.Data;
using LibreLancer.ImUI;
using LibreLancer;
using LibreLancer.Infocards;
using ImGuiNET;
    
namespace LancerEdit
{
    public class InfocardBrowserTab : EditorTab
    {
        int currentString = -1;
        int currentInfocard = -1;

        bool showStrings = true;
        InfocardManager manager;
        InfocardControl display;
        int[] stringsIds;
        int[] infocardsIds;

        TextBuffer txt;

        ListClipper stringClipper;
        ListClipper infocardClipper;

        MainWindow win;
        private FontManager fonts;
        public InfocardBrowserTab(string flini, MainWindow win)
        {
            this.win = win;
            var ini = new FreelancerIni(flini, null);
            fonts = new FontManager();
            var rootFolder = Path.Combine(Path.GetDirectoryName(flini), "../");
            var vfs = FileSystem.FromFolder(rootFolder, true);
            fonts.LoadFontsFromIni(ini, vfs);
            if (ini.JsonResources != null)
                manager = new InfocardManager(File.ReadAllText(ini.JsonResources.Item1), File.ReadAllText(ini.JsonResources.Item2));
            else
                manager = new InfocardManager(ini.Resources);
            stringsIds = manager.StringIds.ToArray();
            infocardsIds = manager.InfocardIds.ToArray();
            txt = new TextBuffer(8192);
            stringClipper = new ListClipper(stringsIds.Length);
            infocardClipper = new ListClipper(infocardsIds.Length);
            Title = "Infocard Browser";
        }

        void GotoString()
        {
            for(int i = 0; i < stringsIds.Length; i++)
            {
                if(id == stringsIds[i]) {
                    gotoItem = i;
                    currentString = i;
                    txt.SetText(manager.GetStringResource(stringsIds[i]));
                }
            }
        }

        void GotoInfocard()
        {
            for (int i = 0; i < infocardsIds.Length; i++)
            {
                if (id == infocardsIds[i])
                {
                    gotoItem = i;
                    currentInfocard = i;
                    if (display == null)
                    {
                        display = new InfocardControl(win, RDLParse.Parse(manager.GetXmlResource(infocardsIds[currentInfocard]), fonts), 100);
                    }
                    else
                    {
                        display.SetInfocard(RDLParse.Parse(manager.GetXmlResource(infocardsIds[currentInfocard]), fonts));
                    }
                }
            }
        }

        private int gotoItem = -1;
        int id = 0;

        public override void Draw()
        {
            SearchDialog();
            ImGui.Columns(2, "cols", true);
            //strings vs infocards
            if (ImGuiExt.ToggleButton("Strings", showStrings)) showStrings = true;
            ImGui.SameLine();
            if (ImGuiExt.ToggleButton("Infocards", !showStrings)) showStrings = false;
            ImGui.SameLine();
            ImGui.PushItemWidth(140);
            ImGui.InputInt("##id", ref id, 0, 0);
            ImGui.PopItemWidth();
            ImGui.SameLine();
            if (ImGui.Button("Go"))
            {
                if (showStrings)
                {
                    GotoString();
                }
                else
                {
                    GotoInfocard();
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Search..."))
            {
                if (showStrings) SearchStrings();
                else SearchInfocards();
            }
            ImGui.Separator();
            //list
            ImGui.BeginChild("##list");
            if(showStrings)
            {
                if (gotoItem == -1)
                {
                    stringClipper.Begin(stringsIds.Length);
                    while (stringClipper.Step())
                    {
                        for (int i = stringClipper.DisplayStart; i < stringClipper.DisplayEnd; i++)
                        {
                            if (ImGui.Selectable(stringsIds[i] + "##" + i, currentString == i))
                            {
                                currentString = i;
                                txt.SetText(manager.GetStringResource(stringsIds[i]));
                            }
                        }
                    }
                    stringClipper.End();
                }
                else
                {
                    for(int i = 0; i < stringsIds.Length; i++)
                    {
                        ImGui.Selectable(stringsIds[i] + "##" + i, currentString == i);
                        if (currentString == i) ImGui.SetScrollHereY();
                    }
                    gotoItem = -1;
                }
            } else {
                if (gotoItem == -1)
                {
                    infocardClipper.Begin(infocardsIds.Length);
                    while (infocardClipper.Step())
                    {
                        for (int i = infocardClipper.DisplayStart; i < infocardClipper.DisplayEnd; i++)
                        {
                            if (ImGui.Selectable(infocardsIds[i] + "##" + i, currentInfocard == i))
                            {
                                currentInfocard = i;
                                if (display == null)
                                {
                                    display = new InfocardControl(win, RDLParse.Parse(manager.GetXmlResource(infocardsIds[currentInfocard]), fonts), 100);
                                }
                                else
                                {
                                    display.SetInfocard(RDLParse.Parse(manager.GetXmlResource(infocardsIds[currentInfocard]), fonts));
                                }
                            }
                        }
                    }
                    infocardClipper.End();
                }
                else
                {
                    for (int i = 0; i < infocardsIds.Length; i++)
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
                    ImGui.Text(stringsIds[currentString].ToString());
                    txt.InputTextMultiline("##txt", new Vector2(-1, ImGui.GetWindowHeight() - 70), ImGuiInputTextFlags.ReadOnly);
                }
            }
            else
            {
                if(currentInfocard != -1)
                {
                    ImGui.Text(infocardsIds[currentInfocard].ToString());
                    ImGui.BeginChild("##display");
                    display.Draw(ImGui.GetWindowWidth() - 15);
                    ImGui.EndChild();
                }
            }
        }

        private bool isSearchInfocards;
        private bool searchWholeWord = false;
        private bool searchCaseSensitive = false;
        private volatile int dialogState = 0;
        private TextBuffer searchBuffer = new TextBuffer();
        private int[] searchResults;
        private string[] searchStrings;
        private string[] searchStringPreviews;
        private bool doOpenSearch;
        private string resultTitle;
        void SearchStrings()
        {
            dialogState = 0;
            isSearchInfocards = false;
            searchBuffer.Clear();
            doOpenSearch = true;
        }
        void SearchInfocards()
        {
            dialogState = 0;
            isSearchInfocards = true;
            searchBuffer.Clear();
            doOpenSearch = true;
        }

        private const int MAX_PREV_LEN = 50;
        string EllipseIfNeeded(string s)
        {
            if (s.Length > MAX_PREV_LEN)
            {
                return s.Substring(0, MAX_PREV_LEN) + "...";
            }
            return s;
        }

        private bool searchDlgOpen = false;
        void SearchDialog()
        {
            if (doOpenSearch)
            {
                ImGui.OpenPopup(ImGuiExt.IDWithExtra("Search", Unique));
                doOpenSearch = false;
                searchDlgOpen = true;
            }
            if (ImGui.BeginPopupModal(ImGuiExt.IDWithExtra("Search", Unique), ref searchDlgOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (dialogState == 0) SearchWindow();
                else if (dialogState == 1) SearchStatus();
                else
                {
                    ImGui.Text(resultTitle);
                    ImGui.BeginChild("##results", new Vector2(200,200), true);
                    for(int i = 0; i < searchResults.Length; i++)
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
                                {
                                    try
                                    {
                                        searchStringPreviews[i] =
                                            EllipseIfNeeded(RDLParse.Parse(searchStrings[i], fonts).ExtractText());
                                    }
                                    catch (Exception)
                                    {
                                        searchStringPreviews[i] = EllipseIfNeeded(searchStrings[i]);
                                    }
                                }
                                ImGui.SetTooltip(searchStringPreviews[i]);
                            }
                            else
                                ImGui.SetTooltip(EllipseIfNeeded(searchStrings[i]));    
                        }
                    }

                    ImGui.EndChild();
                }

                ImGui.EndPopup();
            }
        }

        void SearchWindow()
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
                    {
                        AsyncManager.RunTask(() =>
                        {
                            var results = new List<int>();
                            var resStrings = new List<string>();
                            foreach (var kv in manager.AllXml)
                            {
                                if (r.IsMatch(kv.Value))
                                {
                                    results.Add(kv.Key);
                                    resStrings.Add(kv.Value);
                                }
                            }
                            searchResults = results.ToArray();
                            searchStrings = resStrings.ToArray();
                            searchStringPreviews = new string[searchStrings.Length];
                            dialogState = 2;
                        });   
                    }
                    else
                    {
                        AsyncManager.RunTask(() =>
                        {
                            var results = new List<int>();
                            var resStrings = new List<string>();
                            foreach (var kv in manager.AllStrings)
                            {
                                if (r.IsMatch(kv.Value))
                                {
                                    results.Add(kv.Key);
                                    resStrings.Add(kv.Value);
                                }
                            }
                            searchResults = results.ToArray();
                            searchStrings = resStrings.ToArray();
                            dialogState = 2;
                        });
                    }
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
        }

        void SearchStatus()
        {
            ImGuiExt.Spinner("##spinner", 10, 2, ImGuiNative.igGetColorU32(ImGuiCol.ButtonHovered, 1));
            ImGui.SameLine();
            ImGui.Text("Searching");
        }
        
        public override void Dispose()
        {
            stringClipper.Dispose();
            infocardClipper.Dispose();
        }
    }
}
