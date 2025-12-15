using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ImGuiNET;

namespace LLServer
{
    public static  class GuiHelpers
    {
        public static void CenterText(string text)
        {
            ImGui.Dummy(new Vector2(1));
            var win = ImGui.GetWindowWidth();
            var txt = ImGui.CalcTextSize(text).X;
            ImGui.SameLine(Math.Max((win / 2f) - (txt / 2f), 0));
            ImGui.Text(text);
        }
        public static void CenterText(string text, Vector4 colour)
        {
            ImGui.Dummy(new Vector2(1));
            var win = ImGui.GetWindowWidth();
            var txt = ImGui.CalcTextSize(text).X;
            ImGui.SameLine(Math.Max((win / 2f) - (txt / 2f), 0));
            ImGui.TextColored(colour, text);

        }
    }
}
