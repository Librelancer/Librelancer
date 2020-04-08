// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using ImGuiNET;
namespace LibreLancer.ImUI
{
    public unsafe class TabHandler
    {
        public static void TabLabels(List<DockTab> tabs, ref DockTab selected)
        {
            if (tabs.Count > 0)
            {
                var flags = ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.FittingPolicyScroll |
                            ImGuiTabBarFlags.AutoSelectNewTabs | ImGuiTabBarFlags.TabListPopupButton;
                ImGui.BeginTabBar("##tabbar", flags);
                for (int i = 0; i < tabs.Count; i++)
                {
                    bool isTabOpen = true;
                    bool selectedThis = false;
                    if (ImGui.BeginTabItem(tabs[i].RenderTitle, ref isTabOpen, ImGuiTabItemFlags.None))
                    {
                        selectedThis = true;
                        ImGui.EndTabItem();
                    }
                    if (!isTabOpen)
                    {
                        if(selected == tabs[i]) selected = null;
                        tabs[i].Dispose();
                        tabs.RemoveAt(i);
                    }
                    else if (selectedThis)
                        selected = tabs[i];
                }

                ImGui.EndTabBar();
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
            var pos = (Vector2)ImGui.GetCursorScreenPos() + new Vector2(pad, textSize.X + pad);
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
