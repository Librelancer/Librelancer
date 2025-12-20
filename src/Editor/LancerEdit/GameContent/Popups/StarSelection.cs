using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.GameData.Archetypes;
using LibreLancer.Graphics;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class StarSelection : PopupWindow
{
    public override string Title { get; set; } = "Star";

    public Sun Selected;
    private string filterText = "";
    private Sun[] displayList;
    private Sun[] fullList;
    private GameDataContext gd;


    private bool doFiltering = false;

    private ImGuiInputTextCallback textCallback;
    private RenderContext renderContext;
    private Action<Sun> onSelect;
    private SunImmediateRenderer sunPreview;


    unsafe int OnTextChanged(ImGuiInputTextCallbackData* d)
    {
        doFiltering = true;
        return 0;
    }

    public override Vector2 InitSize => new Vector2(600, 400) * ImGuiHelper.Scale;


    public unsafe StarSelection(Action<Sun> onSelect, Sun selection, SunImmediateRenderer sunPreview, GameDataContext gd, RenderContext rc)
    {
        displayList = fullList = gd.GameData.Items.Stars.OrderBy(x => x.Nickname).ToArray();
        Selected = selection;
        this.sunPreview = sunPreview;
        this.onSelect = onSelect;
        this.renderContext = rc;
        textCallback = OnTextChanged;
    }

    public override void Draw(bool appearing)
    {
        if (DrawTable("##suns", out var sel))
        {
            onSelect(sel);
            ImGui.CloseCurrentPopup();
        }
    }

    public override void OnClosed()
    {
        sunPreview.Dispose();
        sunPreview = null;
    }

    unsafe bool SunButton(Sun sun, Vector2 size, bool isSelected)
    {
        ImGui.PushID(sun.Nickname);
        uint col = isSelected ? ImGui.GetColorU32(ImGuiCol.ButtonActive) : ImGui.GetColorU32(ImGuiCol.FrameBg);
        var retval = ImGui.InvisibleButton("##selected", size);
        if (ImGui.IsItemHovered()) {
            col = ImGui.GetColorU32(ImGuiCol.ButtonHovered);
        }
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var r = new Rectangle((int)min.X, (int)min.Y, (int)(max.X - min.X), (int)(max.Y - min.Y));
        var dl = ImGui.GetWindowDrawList();
        dl.AddCallback((_, cmd) =>
        {
            if (sunPreview == null)
                return;
            renderContext.PushScissor(ImGuiHelper.GetClipRect(cmd), false);
            sunPreview.Render(sun, (Color4)(VertexDiffuse)col, renderContext, r);
            renderContext.PopScissor();
        }, IntPtr.Zero);

        dl.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Border));
        ImGui.PopID();
        return retval;
    }

    bool DrawTable(string id, out Sun returnValue)
    {
        ImGui.PushID(id);
        ImGui.PushItemWidth(-1);
        ImGui.InputTextWithHint("##filter", "Filter", ref filterText, 250, ImGuiInputTextFlags.CallbackEdit,
            textCallback);
        ImGui.PopItemWidth();
        if (doFiltering)
        {
            if (string.IsNullOrEmpty(filterText))
            {
                displayList = fullList;
            }
            else
            {
                displayList = fullList.Where(
                        x => x == Selected || x.Nickname.Contains(filterText, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            doFiltering = false;
        }

        bool doReturn = false;
        returnValue = null;
        ImGui.BeginChild("##archetypes", new Vector2(ImGui.GetWindowWidth() - 12 * ImGuiHelper.Scale,
            ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - ImGui.GetFrameHeightWithSpacing() - 8 * ImGuiHelper.Scale), ImGuiChildFlags.Borders);
        ImGui.BeginTable("##table", 4);
        var clipper = new ImGuiListClipper();
        int itemsLen = (displayList.Length + 1) / 4;
        if ((displayList.Length + 1) % 4 != 0) itemsLen++;
        clipper.Begin(itemsLen);
        while (clipper.Step())
        {
            for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
            {
                ImGui.TableNextRow();
                for (int i = row * 4; i < (row + 1) * 4 && i < (displayList.Length + 1); i++)
                {
                    ImGui.TableNextColumn();
                    if (i == displayList.Length)
                    {
                        bool isNull = Selected == null;
                        if (isNull) ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));
                        if (ImGui.Button("##=null", new Vector2(80 * ImGuiHelper.Scale)))
                        {
                            Selected = returnValue = null;
                            doReturn = true;
                            doFiltering = true;
                        }
                        if(isNull) ImGui.PopStyleColor();
                        ImGui.Text("(none)");
                        continue;
                    }
                    if (SunButton(displayList[i], new Vector2(80) * ImGuiHelper.Scale, displayList[i] == Selected))
                    {
                        Selected = returnValue = displayList[i];
                        doReturn = true;
                        doFiltering = true;
                    }
                    ImGui.Text(displayList[i].Nickname);
                }
            }
        }

        ImGui.EndTable();
        ImGui.EndChild();
        ImGui.PopID();
        return doReturn;
    }




}
