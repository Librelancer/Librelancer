// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.Popups;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Graphics.Text;
using LibreLancer.ImUI;
using LibreLancer.Infocards;

namespace LancerEdit.GameContent;

public class InfocardBrowserTab : GameContentTab
{
    private readonly Infocard blankInfocard = new()
    {
        Nodes = new List<RichTextNode>(new[] {new RichTextTextNode {FontName = "Arial", FontSize = 12, Contents = ""}})
    };

    private int currentInfocard = -1;
    private int currentString = -1;
    private string currentXml;
    private readonly InfocardControl display;
    private readonly FontManager fonts;

    private int gotoItem = -1;
    private int id;
    private int[] infocardsIds;
    private bool isSearchInfocards;
    private readonly EditableInfocardManager manager;

    private bool showStrings = true;

    private int[] stringsIds;

    private readonly MainWindow win;
    private bool xmlDlgOpen;

    private PopupManager popups = new PopupManager();

    private bool showDllList = false;

    public InfocardBrowserTab(GameDataContext gameData, MainWindow win)
    {
        this.win = win;
        fonts = gameData.Fonts;
        manager = gameData.Infocards;
        ResetListContent();
        Title = "Infocard Browser";
        display = new InfocardControl(win, blankInfocard, 100);
    }

    void ResetListContent()
    {
        stringsIds = manager.StringIds.ToArray();
        infocardsIds = manager.InfocardIds.ToArray();
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

    public override void Draw(double elapsed)
    {
        if (editingXml)
        {
            popups.Run();
            InfocardEditing();
            return;
        }

        popups.Run();
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
            if (showStrings)
                popups.OpenPopup(IdsSearch.SearchStrings(manager, fonts, x =>
                {
                    id = x;
                    GotoString();
                }));
            else
                popups.OpenPopup(IdsSearch.SearchInfocards(manager, fonts, x =>
                {
                    id = x;
                    GotoInfocard();
                }));
        }

        ImGui.SameLine();
        if (ImGui.Button("Add"))
            popups.OpenPopup(new AddIdsPopup(this, !showStrings));

        ImGui.SameLine();
        if (ImGuiExt.Button("Save", manager.Dirty))
            manager.Save();

        ImGui.SameLine();
        if (ImGuiExt.Button("Clear Changes", manager.Dirty)) {
            manager.Reset();
            ResetListContent();
        }

        ImGui.SameLine();
        if (ImGuiExt.ToggleButton("Dll List", showDllList)) showDllList = !showDllList;
        ImGui.Separator();
        if (showDllList && ImGui.Begin("Dll List", ref showDllList))
        {
            ImGui.PushFont(ImGuiHelper.SystemMonospace, 0);
            for (int i = 0; i < manager.Dlls.Count; i++) {
                ImGui.Text($"{i * 65536} - {i * 65536 + 65535}: {Path.GetFileName(manager.Dlls[i].SavePath)}");
            }
            ImGui.PopFont();
            ImGui.End();
        }
        ImGui.Columns(2, "cols", true);
        //list
        ImGui.BeginChild("##list");
        if (showStrings)
        {
            if (gotoItem == -1)
            {
                var stringClipper = new ImGuiListClipper();
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
                var infocardClipper = new ImGuiListClipper();
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
                ImGui.Text(stringsIds[currentString].ToString());
                ImGui.SameLine();
                if (ImGui.Button("Copy Text"))
                    win.SetClipboardText(manager.GetStringResource(stringsIds[currentString]));
                ImGui.SameLine();
                if (ImGui.Button("Edit"))
                {
                    popups.OpenPopup(new EditStringPopup(this, stringsIds[currentString]));
                }
                ImGui.SameLine();
                if(ImGui.Button("Delete"))
                    DeleteString(stringsIds[currentString]);
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
                ImGui.Text(infocardsIds[currentInfocard].ToString());
                ImGui.SameLine();
                if (ImGui.Button("View Xml")) popups.OpenPopup(new XmlPopup(win, currentXml));
                ImGui.SameLine();
                if (ImGui.Button("Copy Text")) win.SetClipboardText(display.InfocardText);
                ImGui.SameLine();
                if (ImGui.Button("Edit"))
                {
                    xmlEditIds = infocardsIds[currentInfocard];
                    xmlEditText = XmlFormatter.Prettify(manager.GetXmlResource(infocardsIds[currentInfocard]));
                    editingXml = true;
                }
                ImGui.SameLine();
                if(ImGui.Button("Delete"))
                    DeleteInfocard(infocardsIds[currentInfocard]);
                ImGui.BeginChild("##display");
                display.Draw(ImGui.GetWindowWidth() - 15);
                ImGui.EndChild();
            }
        }
    }

    void DeleteString(int strid)
    {
        win.Confirm($"Delete {strid}?", () =>
        {
            currentString = -1;
            manager.RemoveStringResource(strid);
            ResetListContent();
        });
    }

    void DeleteInfocard(int ifc)
    {
        win.Confirm($"Delete {ifc}?", () =>
        {
            currentInfocard = -1;
            manager.RemoveXmlResource(ifc);
            ResetListContent();
        });
    }

    private InfocardControl xmlEditPreview = null;
    private string xmlPreviewText = "";
    private Infocard previewInfocard;
    private string xmlEditText;
    private int xmlEditIds;
    private bool editingXml = false;

    private void InfocardEditing()
    {
        ImGui.Text($"Editing: {xmlEditIds}");
        if (ImGui.Button("Save"))
        {
            manager.SetXmlResource(xmlEditIds, XmlFormatter.Minimize(xmlEditText));
            DisplayInfoXml();
            editingXml = false;
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            editingXml = false;
        }

        ImGui.Columns(2);
        ImGui.BeginChild("##edit");
        ImGui.Text("Xml");
        ImGui.PushFont(ImGuiHelper.SystemMonospace, 0);
        ImGui.InputTextMultiline("##xmltext", ref xmlEditText, 800000, new Vector2(
            ImGui.GetColumnWidth() - 2 * ImGuiHelper.Scale,
            ImGui.GetWindowHeight() - 40 * ImGuiHelper.Scale));
        ImGui.PopFont();
        ImGui.EndChild();
        ImGui.NextColumn();
        ImGui.BeginChild("##display");
        ImGui.Text("Preview");
        if (xmlPreviewText != xmlEditText ||
            previewInfocard == null)
        {
            previewInfocard = RDLParse.Parse(xmlEditText, fonts);
            xmlPreviewText = xmlEditText;
            xmlEditPreview?.SetInfocard(previewInfocard);
        }

        if (xmlEditPreview == null)
            xmlEditPreview = new InfocardControl(win, previewInfocard, ImGui.GetColumnWidth());
        xmlEditPreview?.Draw(ImGui.GetColumnWidth() - 2 * ImGuiHelper.Scale);
        ImGui.EndChild();
        ImGui.Columns(1);
    }

    sealed class EditStringPopup : PopupWindow
    {
        public override string Title { get; set; } = "Edit String";

        private string text;
        private string original;
        private int ids;
        private InfocardBrowserTab tab;

        public EditStringPopup(InfocardBrowserTab tab, int ids)
        {
            this.ids = ids;
            this.tab = tab;
            text = original = tab.manager.GetStringResource(ids);
        }

        public override void Draw(bool appearing)
        {
            ImGui.Text($"Editing: {ids}");
            ImGui.Text($"Original: {original}");
            ImGui.InputTextMultiline("New Text", ref text, ushort.MaxValue, Vector2.Zero);
            if (ImGui.Button("Save"))
            {
                tab.manager.SetStringResource(ids, text);
                tab.DisplayInfoString();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
        }
    }

    sealed class AddIdsPopup : PopupWindow
    {
        public override string Title { get; set; }

        public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;
        private InfocardBrowserTab tab;
        private bool isInfocard;
        private int newIds;

        public AddIdsPopup(InfocardBrowserTab tab, bool isInfocard)
        {
            this.tab = tab;
            this.isInfocard = isInfocard;
            Title = isInfocard ? "Add Infocard" : "Add String";
        }

        public override void Draw(bool appearing)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("New Id");
            ImGui.PushItemWidth(130 * ImGuiHelper.Scale);
            ImGui.InputInt("##newids", ref newIds, 0, 0);
            ImGui.PopItemWidth();
            ImGui.SameLine();
            if (ImGui.Button("Next"))
            {
                var r = tab.manager.NextFreeId(newIds);
                if (r == -1)
                    tab.win.ErrorDialog("No id available. Add a new .dll file.");
                else
                    newIds = r;
            }
            bool canSave = false;
            if (newIds <= 0)
            {
                ImGui.TextColored(Color4.Red, "Id must be 1 or larger");
            }
            else if (newIds > tab.manager.MaxIds)
            {
                ImGui.TextColored(Color4.Red,
                    $"{newIds} is bigger than max id {tab.manager.MaxIds}.\nAdd a new .dll file.");
            }
            else if (tab.manager.StringExists(newIds))
            {
                ImGui.TextColored(Color4.Red, "Id already in use (string)");
            }
            else if (tab.manager.XmlExists(newIds))
            {
                ImGui.TextColored(Color4.Red, "Id already in use (infocard)");
            }
            else
            {
                ImGui.TextDisabled($"Dll: {Path.GetFileName(tab.manager.Dlls[newIds >> 16].SavePath)}");
                canSave = true;
            }
            if (ImGuiExt.Button("Save", canSave))
            {
                tab.id = newIds;
                if (isInfocard)
                {
                    tab.manager.SetXmlResource(newIds, "<RDL><PUSH/><TEXT>New Infocard</TEXT></RDL>");
                    tab.ResetListContent();
                    tab.GotoInfocard();
                }
                else
                {
                    tab.manager.SetStringResource(newIds, "New String");
                    tab.ResetListContent();
                    tab.GotoString();
                }

                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
        }
    }

    class XmlPopup(MainWindow win, string xmlText) : PopupWindow
    {
        private string xmlText = xmlText;
        public override string Title { get; set; } = "Xml";
        public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;
        public override void Draw(bool appearing)
        {
            if (ImGui.Button("Copy To Clipboard")) win.SetClipboardText(xmlText);
            ImGui.PushFont(ImGuiHelper.SystemMonospace, 0);
            ImGui.InputTextMultiline("##xml", ref xmlText, uint.MaxValue, new Vector2(400),
                ImGuiInputTextFlags.ReadOnly);
            ImGui.PopFont();
        }
    }

    public override void Dispose()
    {
        display.Dispose();
        xmlEditPreview?.Dispose();
    }
}
