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
    private readonly ListClipper infocardClipper;
    private int[] infocardsIds;
    private bool isSearchInfocards;
    private readonly EditableInfocardManager manager;

    private bool showStrings = true;

    private readonly ListClipper stringClipper;
    private int[] stringsIds;

    private readonly MainWindow win;
    private bool xmlDlgOpen;

    private PopupManager popups = new PopupManager();
    private const string Popup_InfocardXml = "Xml";
    private const string Popup_EditString = "Edit String";
    private const string Popup_AddString = "Add String";
    private const string Popup_AddInfocard = "Add Infocard";

    private bool showDllList = false;


    public InfocardBrowserTab(GameDataContext gameData, MainWindow win)
    {
        this.win = win;
        fonts = gameData.Fonts;
        manager = gameData.Infocards;
        ResetListContent();
        stringClipper = new ListClipper();
        infocardClipper = new ListClipper();
        Title = "Infocard Browser";
        display = new InfocardControl(win, blankInfocard, 100);
        popups.AddPopup<string>(Popup_InfocardXml, InfocardXmlDialog, ImGuiWindowFlags.AlwaysAutoResize);
        popups.AddPopup<EditingStringState>(Popup_EditString, EditStringPopup);
        popups.AddPopup<AddState>(Popup_AddString, (p, a) => AddPopup(a, false), ImGuiWindowFlags.AlwaysAutoResize);
        popups.AddPopup<AddState>(Popup_AddInfocard, (p, a) => AddPopup(a, true), ImGuiWindowFlags.AlwaysAutoResize);
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
            popups.OpenPopup(showStrings ? Popup_AddString : Popup_AddInfocard, new AddState());

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
            ImGui.PushFont(ImGuiHelper.SystemMonospace);
            for (int i = 0; i < manager.Dlls.Count; i++) {
                ImGui.TextUnformatted($"{i * 65536} - {i * 65536 + 65535}: {Path.GetFileName(manager.Dlls[i].SavePath)}");
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
                ImGui.SameLine();
                if (ImGui.Button("Edit"))
                {
                    popups.OpenPopup(Popup_EditString, new EditingStringState(
                        stringsIds[currentString],
                        manager.GetStringResource(stringsIds[currentString])
                    ));
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
                ImGui.TextUnformatted(infocardsIds[currentInfocard].ToString());
                ImGui.SameLine();
                if (ImGui.Button("View Xml")) popups.OpenPopup(Popup_InfocardXml, currentXml);
                ImGui.SameLine();
                if (ImGui.Button("Copy Text")) win.SetClipboardText(display.InfocardText);
                ImGui.SameLine();
                if (ImGui.Button("Edit"))
                {
                    xmlState = new EditingStringState(
                        infocardsIds[currentInfocard],
                        XmlFormatter.Prettify(manager.GetXmlResource(infocardsIds[currentInfocard]))
                    );
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

    class EditingStringState
    {
        public int Ids;
        public string Text;

        public EditingStringState(int ids, string text)
        {
            Ids = ids;
            Text = text;
        }
    }


    private InfocardControl xmlEditPreview = null;
    private string xmlPreviewText = "";
    private Infocard previewInfocard;
    private EditingStringState xmlState;
    private bool editingXml = false;

    private void InfocardEditing()
    {
        ImGui.Text($"Editing: {xmlState.Ids}");
        if (ImGui.Button("Save"))
        {
            manager.SetXmlResource(xmlState.Ids, XmlFormatter.Minimize(xmlState.Text));
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
        ImGui.PushFont(ImGuiHelper.SystemMonospace);
        ImGui.InputTextMultiline("##xmltext", ref xmlState.Text, 800000, new Vector2(
            ImGui.GetColumnWidth() - 2 * ImGuiHelper.Scale,
            ImGui.GetWindowHeight() - 40 * ImGuiHelper.Scale));
        ImGui.PopFont();
        ImGui.EndChild();
        ImGui.NextColumn();
        ImGui.BeginChild("##display");
        ImGui.Text("Preview");
        if (xmlPreviewText != xmlState.Text ||
            previewInfocard == null)
        {
            previewInfocard = RDLParse.Parse(xmlState.Text, fonts);
            xmlPreviewText = xmlState.Text;
            xmlEditPreview?.SetInfocard(previewInfocard);
        }

        if (xmlEditPreview == null)
            xmlEditPreview = new InfocardControl(win, previewInfocard, ImGui.GetColumnWidth());
        xmlEditPreview?.Draw(ImGui.GetColumnWidth() - 2 * ImGuiHelper.Scale);
        ImGui.EndChild();
        ImGui.Columns(1);
    }

    private void EditStringPopup(PopupData data, EditingStringState state)
    {
        ImGui.Text($"Editing: {state.Ids}");
        ImGui.TextUnformatted($"Original: {manager.GetStringResource(state.Ids)}");
        ImGui.InputTextMultiline("New Text", ref state.Text, ushort.MaxValue, Vector2.Zero);
        if (ImGui.Button("Save"))
        {
            manager.SetStringResource(state.Ids, state.Text);
            DisplayInfoString();
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }
    }

    class AddState
    {
        public int NewIds;
    }

    private void AddPopup(AddState state, bool isInfocard)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text("New Id");
        ImGui.PushItemWidth(130 * ImGuiHelper.Scale);
        ImGui.InputInt("##newids", ref state.NewIds, 0, 0);
        ImGui.PopItemWidth();
        ImGui.SameLine();
        if (ImGui.Button("Next"))
        {
            var r = manager.NextFreeId(state.NewIds);
            if (r == -1)
                win.ErrorDialog("No id available. Add a new .dll file.");
            else
                state.NewIds = r;
        }
        bool canSave = false;
        if (state.NewIds <= 0)
        {
            ImGui.TextColored(Color4.Red, "Id must be 1 or larger");
        }
        else if (state.NewIds > manager.MaxIds)
        {
            ImGui.TextColored(Color4.Red,
                $"{state.NewIds} is bigger than max id {manager.MaxIds}.\nAdd a new .dll file.");
        }
        else if (manager.StringExists(state.NewIds))
        {
            ImGui.TextColored(Color4.Red, "Id already in use (string)");
        }
        else if (manager.XmlExists(state.NewIds))
        {
            ImGui.TextColored(Color4.Red, "Id already in use (infocard)");
        }
        else
        {
            ImGui.TextDisabled($"Dll: {Path.GetFileName(manager.Dlls[state.NewIds >> 16].SavePath)}");
            canSave = true;
        }
        if (ImGuiExt.Button("Save", canSave))
        {
            id = state.NewIds;
            if (isInfocard)
            {
                manager.SetXmlResource(state.NewIds, "<RDL><PUSH/><TEXT>New Infocard</TEXT></RDL>");
                ResetListContent();
                GotoInfocard();
            }
            else
            {
                manager.SetStringResource(state.NewIds, "New String");
                ResetListContent();
                GotoString();
            }

            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }
    }


    private void InfocardXmlDialog(PopupData data, string xmlText)
    {
        if (ImGui.Button("Copy To Clipboard")) win.SetClipboardText(xmlText);
        ImGui.PushFont(ImGuiHelper.SystemMonospace);
        ImGui.InputTextMultiline("##xml", ref xmlText, uint.MaxValue, new Vector2(400),
            ImGuiInputTextFlags.ReadOnly);
        ImGui.PopFont();
    }

    public override void Dispose()
    {
        stringClipper.Dispose();
        infocardClipper.Dispose();
        display.Dispose();
        xmlEditPreview?.Dispose();
    }
}
