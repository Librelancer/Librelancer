// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using ImGuiNET;

namespace LibreLancer.ImUI
{
    public static class Theme
    {
        static void SetColor(ImGuiCol col, Vector4 rgba)
        {
            ImGui.GetStyle().Colors[(int)col] = rgba;
        }

        public static Vector4 VTabInactive = Rgba(56, 56, 56, 255);
        public static Vector4 VTabActive = Rgba(95, 97, 98, 255);
        public static Vector4 WorkspaceBackground = Rgba(34, 34, 34, 255);

        public static readonly Vector4 ErrorTextColor = new(1f, 0.3f, 0.3f, 1f);
        public static readonly Vector4 WarnTextColor = new(1f, 0.86f, 0.25f, 1f);
        public static readonly Vector4 SuccessTextColor = new(0f, 0.8f, 0.2f, 1f);

        public static readonly Vector4 ErrorInputColor = new Vector4(0.4f, 0.05f, 0.05f, 1f);
        public static readonly Vector4 ErrorInputHoverColor = new Vector4(0.6f, 0.1f, 0.1f, 1f);
        public static readonly Vector4 ErrorInputActiveColor = new Vector4(0.7f, 0.1f, 0.1f, 1f);

        public static float LabelWidth => 100f * ImGuiHelper.Scale;
        public static float LabelWidthMedium => 125f * ImGuiHelper.Scale;
        public static float LabelWidthLong => 135f * ImGuiHelper.Scale;
        public static float ButtonWidth => 110f * ImGuiHelper.Scale;
        public static float ButtonWidthMedium => 120f * ImGuiHelper.Scale;
        public static float ButtonWidthLong => 180f * ImGuiHelper.Scale;
        public static float SquareButtonWidth => 30 * ImGuiHelper.Scale;
        public static float ButtonPadding => 16 * ImGuiHelper.Scale;


        private static float _currentScale = -1;
        private static ImGuiStyle _savedStyle;
        private static bool _inited = false;

        public const float FontSizeBase = 15f;

        static unsafe void Init()
        {
            if (_inited)
                return;
            _inited = true;
            var s = ImGui.GetStyle();
            //Settings
            s.TreeLinesFlags = ImGuiTreeNodeFlags.DrawLinesToNodes;
            s.FrameRounding = 2;
            s.ScrollbarSize = 12;
            s.ScrollbarRounding = 3;
            s.FrameBorderSize = 1f;
            s.Alpha = 1;
            //Colours
            SetColor(ImGuiCol.WindowBg, Rgba(41, 41, 42, 210));
            SetColor(ImGuiCol.ChildBg, Rgba(0, 0, 0, 0));
            SetColor(ImGuiCol.Border, Rgba(83, 83, 83, 255));
            SetColor(ImGuiCol.BorderShadow, Rgba(0, 0, 0, 0));
            SetColor(ImGuiCol.FrameBg, Rgba(48, 48, 48, 255));
            SetColor(ImGuiCol.PopupBg, Rgba(48, 48, 48, 255));
            SetColor(ImGuiCol.FrameBgHovered, Rgba(66, 133, 190, 255));
            SetColor(ImGuiCol.Header, Rgba(88, 178, 255, 132));
            SetColor(ImGuiCol.HeaderActive, Rgba(88, 178, 255, 164));
            SetColor(ImGuiCol.FrameBgActive, VTabActive);
            SetColor(ImGuiCol.MenuBarBg, Rgba(66, 67, 69, 255));
            SetColor(ImGuiCol.ScrollbarBg, Rgba(51, 64, 77, 153));
            SetColor(ImGuiCol.Button, Rgba(66, 66, 66, 255));
            SetColor(ImGuiCol.TabSelected, Rgba(95, 97, 98, 255));
            SetColor(ImGuiCol.TabHovered, Rgba(66, 133, 190, 255));
            SetColor(ImGuiCol.Tab, Rgba(56, 57, 58, 255));

            _savedStyle = *s.Handle;
        }
        public static unsafe void Apply(float scale)
        {
            Init();
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_currentScale != scale)
            {
                var s = ImGui.GetStyle();
                *s.Handle = _savedStyle;
                s.ScaleAllSizes(scale);
                s.FontScaleDpi = scale;
                s.FontSizeBase = FontSizeBase;
                _currentScale = scale;
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

        static Vector4 Rgba(int r, int g, int b, int a)
        {
            return new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
        }
    }
}
