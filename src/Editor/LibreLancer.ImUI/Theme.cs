// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Linq;
using LibreLancer.ImageLib;
using ImGuiNET;
namespace LibreLancer.ImUI
{
    public static class Theme
    {
        static void SetColor(ImGuiCol col, Vector4 rgba)
        {
            ImGui.GetStyle().Colors[(int)col] = rgba;
        }

        public static Vector4 VTabInactive = RGBA(56, 56, 56, 255);
        public static Vector4 VTabActive = RGBA(95, 97, 98, 255);
        public static Vector4 WorkspaceBackground = RGBA(34, 34, 34, 255);

        private static float currentScale = -1;
        private static ImGuiStyle savedStyle;
        private static bool inited = false;
        static unsafe void Init()
        {
            if (inited)
                return;
            inited = true;
            var s = ImGui.GetStyle();
            //Settings
            s.TreeLinesFlags = ImGuiTreeNodeFlags.DrawLinesToNodes;
            s.FrameRounding = 2;
            s.ScrollbarSize = 12;
            s.ScrollbarRounding = 3;
            s.FrameBorderSize = 1f;
            s.Alpha = 1;
            //Colours
            SetColor(ImGuiCol.WindowBg, RGBA(41, 41, 42, 210));
            SetColor(ImGuiCol.ChildBg, RGBA(0, 0, 0, 0));
            SetColor(ImGuiCol.Border, RGBA(83, 83, 83, 255));
            SetColor(ImGuiCol.BorderShadow, RGBA(0, 0, 0, 0));
            SetColor(ImGuiCol.FrameBg, RGBA(48, 48, 48, 255));
            SetColor(ImGuiCol.PopupBg, RGBA(48, 48, 48, 255));
            SetColor(ImGuiCol.FrameBgHovered, RGBA(66, 133, 190, 255));
            SetColor(ImGuiCol.Header, RGBA(88, 178, 255, 132));
            SetColor(ImGuiCol.HeaderActive, RGBA(88, 178, 255, 164));
            SetColor(ImGuiCol.FrameBgActive, VTabActive);
            SetColor(ImGuiCol.MenuBarBg, RGBA(66, 67, 69, 255));
            SetColor(ImGuiCol.ScrollbarBg, RGBA(51, 64, 77, 153));
            SetColor(ImGuiCol.Button, RGBA(66, 66, 66, 255));
            SetColor(ImGuiCol.TabSelected, RGBA(95, 97, 98, 255));
            SetColor(ImGuiCol.TabHovered, RGBA(66, 133, 190, 255));
            SetColor(ImGuiCol.Tab, RGBA(56, 57, 58, 255));

            savedStyle = *s.Handle;
        }
        public static unsafe void Apply(float scale)
        {
            Init();
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (currentScale != scale)
            {
                var s = ImGui.GetStyle();
                *s.Handle = savedStyle;
                s.ScaleAllSizes(scale);
                s.FontScaleDpi = scale;
                s.FontSizeBase = 15f;
                currentScale = scale;
                FLLog.Debug("UI", $"Setting scale to {scale}");
            }
        }

        public static void TinyTriangle(float x, float y)
        {
            var draw = ImGui.GetWindowDrawList();
            var cen = new Vector2(x, y) + (Vector2)ImGui.GetWindowPos();
            draw.AddTriangleFilled(cen + new Vector2(2f, -2f) * ImGuiHelper.Scale, cen + new Vector2(-2f, 2f) * ImGuiHelper.Scale,cen + new Vector2(2f, 2f) * ImGuiHelper.Scale,
                0xFFFFFFFF);
        }

        public static bool IconMenuItem(char icon, string text, bool enabled) =>
            ImGui.MenuItem($"{icon}   {text}", enabled);


        public static void IconMenuToggle(char icon, string text, ref bool v, bool enabled)
            => ImGui.MenuItem($"{icon}   {text}", "", ref v, enabled);

        public static bool IconTreeNode(char icon, string text, ImGuiTreeNodeFlags flags) =>
            ImGui.TreeNodeEx($"{icon} {text}", flags);

        public static bool IconTreeNode(char icon, string text) =>
            ImGui.TreeNode($"{icon} {text}");

        public static bool BeginIconMenu(char icon, string text) => ImGui.BeginMenu($"{icon}   {text}");

        static Vector4 RGBA(int r, int g, int b, int a)
        {
            return new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
        }
    }
}
