// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;

namespace LancerEdit
{
    public class DropdownOption
    {
        public string Name;
        public char Icon;
        public object Tag;

        public DropdownOption(string name, char icon)
        {
            Name = name;
            Icon = icon;
        }
        public DropdownOption(string name, char icon, object tag)
        {
            Name = name;
            Icon = icon;
            Tag = tag;
        }
    }
    public static class ViewerControls
    {
        public static bool GradientButton(string id, Color4 colA, Color4 colB, Vector2 size, bool gradient)
        {
            if (!gradient)
                return ImGui.ColorButton(id, colA, ImGuiColorEditFlags.NoAlpha, size);
            ImGui.PushID(id);
            var img = ImGuiHelper.RenderGradient(colA, colB);
            var retval = ImGui.ImageButton((IntPtr) img, size, new Vector2(0, 1), new Vector2(0, 0), 0);
            ImGui.PopID();
            return retval;
        }
        
        public static void DropdownButton(string id, ref int selected, IReadOnlyList<DropdownOption> options)
        {
            ImGui.PushID(id);
            bool clicked = false;
            string text = $"{options[selected].Icon}  {options[selected].Name}  ";
            var textSize = ImGui.CalcTextSize(text);
            var cpos = ImGuiNative.igGetCursorPosX();
            var cposY = ImGuiNative.igGetCursorPosY();
            clicked = ImGui.Button($"{options[selected].Icon}  {options[selected].Name}  ");
            var style = ImGui.GetStyle();
            var tPos = new Vector2(cpos, cposY) + new Vector2(textSize.X + style.FramePadding.X, textSize.Y);
            Theme.TinyTriangle(tPos.X, tPos.Y);
            if (clicked)
                ImGui.OpenPopup(id + "#popup");
            if(ImGui.BeginPopup(id + "#popup"))
            {
                ImGui.MenuItem(id, false);
                for (int i = 0; i < options.Count; i++)
                {
                    var opt = options[i];
                    if(Theme.IconMenuItem(opt.Icon, opt.Name, true))
                        selected = i;
                }
                ImGui.EndPopup();
            }
            ImGui.PopID();
        }
    }
}