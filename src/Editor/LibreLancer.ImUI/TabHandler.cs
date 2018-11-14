// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using ImGuiNET;
namespace LibreLancer.ImUI
{
    public unsafe class TabHandler
    {
        static int draggingTabIndex = -1, draggingTabTargetIndex = -1;
        static Vector2 draggingtabSize = Vector2.Zero, draggingTabOffset = Vector2.Zero;

        public static void TabLabels(List<DockTab> tabs, ref DockTab selected)
        {
            if (tabs.Count == 0) return;

            var style = ImGui.GetStyle();
            var color = ImGuiNative.igGetColorU32(ImGuiCol.FrameBg, 1);
            var color_active = ImGuiNative.igGetColorU32(ImGuiCol.FrameBgActive, 1);
            var color_hovered = ImGuiNative.igGetColorU32(ImGuiCol.FrameBgHovered, 1);
            var text_color = ImGuiNative.igGetColorU32(ImGuiCol.Text, 1);
            var text_color_disabled = ImGuiNative.igGetColorU32(ImGuiCol.TextDisabled, 1);

            float windowWidth = 0;
            windowWidth = ImGui.GetWindowWidth() - 2 * style.WindowPadding.X - (ImGuiNative.igGetScrollMaxY() > 0 ? style.ScrollbarSize : 0);
            float tab_base = 0;
            var lineheight = ImGuiNative.igGetTextLineHeightWithSpacing();

            bool isMMBreleased = ImGui.IsMouseReleased(2);
            bool isMouseDragging = ImGui.IsMouseDragging(0, 2f);

            var drawList = ImGuiNative.igGetWindowDrawList();

            float totalWidth = 0;
            foreach(var sztab in tabs)
            {
                totalWidth += ImGui.CalcTextSize(sztab.Title).X;
                totalWidth += 28 + style.ItemSpacing.X;
            }
            var winSize = new Vector2(windowWidth, lineheight);
            var cflags = ImGuiWindowFlags.None;
            if(totalWidth > windowWidth) {
                cflags = ImGuiWindowFlags.HorizontalScrollbar;
                ImGuiNative.igSetNextWindowContentSize(new Vector2(totalWidth, 0));
                winSize.Y += style.ScrollbarSize + 2;
            }
            ImGui.BeginChild("##tabbuttons",winSize,false, cflags);
            for (int i = 0; i < tabs.Count; i++)
            {
                if (i == -1) continue;
                //do button
                if(i > 0) ImGui.SameLine(0, 15);
                ImGui.PushID(i);
                var title = tabs[i].Title.Split(new string[] { "##" }, StringSplitOptions.None)[0];
                var textSz = ImGui.CalcTextSize(title).X;
                var size = new Vector2(textSz, lineheight);
                //Selection and hover
                if (ImGui.InvisibleButton(title,size)) { selected = tabs[i]; }
                var itemRectSize = ImGuiNative.igGetItemRectSize();
                bool hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.RectOnly);
                if (hovered) 
                {
                    // tab reordering
                    if (isMouseDragging)
                    {
                        if (draggingTabIndex == -1)
                        {
                            draggingTabIndex = i;
                            draggingtabSize = size;
                            Vector2 mp = ImGui.GetIO().MousePos;
                            var draggingTabCursorPos = ImGui.GetCursorPos();
                            draggingTabOffset = new Vector2(
                                 draggingtabSize.X * 0.5f,
                                 draggingtabSize.Y * 0.5f
                            );

                        }
                    }
                    else if (draggingTabIndex >= 0 && draggingTabIndex < tabs.Count && draggingTabIndex != i)
                    {
                        draggingTabTargetIndex = i; // For some odd reasons this seems to get called only when draggingTabIndex < i ! (Probably during mouse dragging ImGui owns the mouse someway and sometimes ImGui::IsItemHovered() is not getting called)
                    }
                }
                //actually draw
                var pos = (Vector2)ImGui.GetItemRectMin();
                tab_base = pos.Y;
                size.X += 20 + style.ItemSpacing.X;
                ImGuiNative.ImDrawList_AddRectFilled(
                    drawList, pos + new Vector2(-8,0),
                    pos + size,
                    hovered ? color_hovered : selected == tabs[i] ? color_active : color,
                    0,
                    0
                );
                var bytes = System.Text.Encoding.UTF8.GetBytes(title);
                fixed (byte* ptr = bytes)
                {
                    ImGuiNative.ImDrawList_AddText(
                        drawList, pos, text_color, ptr, (byte*)0
                    );
                }
                if (tabs[i] == selected)
                {
                    ImGui.SameLine();
                    if (ImGui.InvisibleButton("close", new Vector2(16, 16)))
                    {
                        if (i > 0) selected = tabs[i - 1];
                        else if (i == 0 && tabs.Count > 1) selected = tabs[i + 1];
                        else selected = null;
                        tabs[i].Dispose();
                        tabs.RemoveAt(i);
                    }
                    var c = ((Vector2)ImGui.GetItemRectMin() +
                                (Vector2)ImGui.GetItemRectMax()) * 0.5f;
                    ImGuiNative.ImDrawList_AddLine(
                           drawList, c - new Vector2(3.5f, 3.5f), c + new Vector2(3.5f, 3.5f), text_color, 1
                       );
                    ImGuiNative.ImDrawList_AddLine(
                        drawList, c + new Vector2(3.5f, -3.5f), c + new Vector2(-3.5f, 3.5f), text_color, 1
                    );
                }
                else
                {
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(16));
                }
                ImGui.PopID();
            }
            ImGui.EndChild();
            // Drop tab label
            if (draggingTabTargetIndex != -1)
            {
                // swap draggingTabIndex and draggingTabTargetIndex in tabOrder
                var tmp = tabs[draggingTabTargetIndex];
                tabs[draggingTabTargetIndex] = tabs[draggingTabIndex];
                tabs[draggingTabIndex] = tmp;
                draggingTabTargetIndex = draggingTabIndex = -1;
            }
            // Reset draggingTabIndex if necessary
            if (!isMouseDragging) draggingTabIndex = -1;

            var basestart = new Vector2(0, tab_base + lineheight);
            var baseEnd = new Vector2(windowWidth, tab_base + lineheight);
            ImGuiNative.ImDrawList_AddLine(drawList, basestart, baseEnd, color, 1);
        }

        public static void DrawTabDrag(List<DockTab> tabs)
        {
            var style = ImGui.GetStyle();
            var color_hovered = ImGuiNative.igGetColorU32(ImGuiCol.FrameBgHovered, 1);
            var drawList = ImGuiNative.igGetOverlayDrawList();
            if (draggingTabIndex >= 0 && draggingTabIndex < tabs.Count)
            {
                var mp = ImGui.GetIO().MousePos;
                var wp = ImGui.GetWindowPos();
                var start = new Vector2(
                    wp.X + mp.X - draggingTabOffset.X - draggingtabSize.X * 0.5f, wp.Y + mp.Y - draggingTabOffset.Y - draggingtabSize.Y * 0.5f
                );
                var end = new Vector2(
                    start.X + draggingtabSize.X + 8, start.Y + draggingtabSize.Y
                );
                ImGuiNative.ImDrawList_AddRectFilled(
                    drawList, start, end, color_hovered, 0, 0
                );
                start.X += style.FramePadding.X; start.Y += style.FramePadding.Y;
                var title = tabs[draggingTabIndex].Title.Split(new string[] { "##" }, StringSplitOptions.None)[0];
                var textbytes = System.Text.Encoding.UTF8.GetBytes(title);
                fixed (byte* ptr = textbytes)
                {
                    ImGuiNative.ImDrawList_AddText(
                        drawList, start, uint.MaxValue, ptr, (byte*)0
                    );
                }
                ImGuiNative.igSetMouseCursor(ImGuiMouseCursor.ResizeAll);

            }

        }
        [StructLayout(LayoutKind.Sequential)]
        struct ImFontGlyph
        {
            public ushort Codepoint;
            public float AdvanceX;
            public float X0, Y0, X1, Y1;
            public float U0, V0, U1, V1;
        }

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        static extern ImFontGlyph* igFontFindGlyph(ImFont* font, char c);

        public static bool VerticalTab(string text, bool v)
        {
            var font = ImGuiNative.igGetFont();
            var dlist = ImGuiNative.igGetWindowDrawList();

            var style = ImGui.GetStyle();
            var text_color = ImGui.GetColorU32(ImGuiCol.Text);
            var color = style.Colors[(int)ImGuiCol.Button];
            if (v) color = style.Colors[(int)ImGuiCol.ButtonActive];

            var textSize = ImGui.CalcTextSize(text);
            float pad = style.FramePadding.X;
            var pos = ImGui.GetCursorScreenPos() + new Vector2(pad, textSize.X + pad);
            ImGui.PushStyleColor(ImGuiCol.Button, color);
            ImGui.PushID(text);
            bool ret = ImGui.Button("", new Vector2(textSize.Y + pad * 2,
                                                    textSize.X + pad * 2));
            ImGui.PopStyleColor();
            foreach(var c in text.Reverse()) {
                var glyph = igFontFindGlyph(font, c);
                ImGuiNative.ImDrawList_PrimReserve(
                dlist, 6, 4
                );
                ImGuiNative.ImDrawList_PrimQuadUV(
                    dlist,
                    pos + new Vector2(font->FontSize - glyph->Y0, -glyph->X0),
                    pos + new Vector2(font->FontSize - glyph->Y0, -glyph->X1),
                    pos + new Vector2(font->FontSize - glyph->Y1, -glyph->X1),
                    pos + new Vector2(font->FontSize - glyph->Y1, -glyph->X0),
                    new Vector2(glyph->U1, glyph->V0),
                    new Vector2(glyph->U0, glyph->V0),
                    new Vector2(glyph->U0, glyph->V1),
                    new Vector2(glyph->U1, glyph->V1),
                    text_color
                );
                pos.Y -= glyph->AdvanceX;
            }
            ImGui.PopID();
            return ret;
        }
    }

}
