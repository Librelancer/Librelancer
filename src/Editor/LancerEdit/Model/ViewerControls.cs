// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace LancerEdit
{
    public class DropdownOption
    {
        public string Name;
        public string Icon;
        public object Tag;

        public DropdownOption(string name, string icon)
        {
            Name = name;
            Icon = icon;
        }
        public DropdownOption(string name, string icon, object tag)
        {
            Name = name;
            Icon = icon;
            Tag = tag;
        }
    }
    public static class ViewerControls
    {
        public static bool GradientButton(string id, Color4 colA, Color4 colB, Vector2 size, ViewportManager vps, bool gradient)
        {
            if (!gradient)
                return ImGui.ColorButton(id, new Vector4(colA.R, colA.G, colA.B, 1), ImGuiColorEditFlags.NoAlpha, size);
            ImGui.PushID(id);
            var img = ImGuiHelper.RenderGradient(vps, colA, colB);
            var retval = ImGui.ImageButton((IntPtr) img, size, new Vector2(0, 1), new Vector2(0, 0), 0);
            ImGui.PopID();
            return retval;
        }

        public static void DropdownButton(string id, ref int selected, IReadOnlyList<DropdownOption> options)
        {
            ImGui.PushID(id);
            bool clicked = false;
            const string PADDING = "       ";
            string text = PADDING + ImGuiExt.IDSafe(options[selected].Name) + "   ";
            var w = ImGui.CalcTextSize(text).X;
            clicked = ImGui.Button(text);
            ImGui.SameLine();
            var cpos = ImGuiNative.igGetCursorPosX();
            var cposY = ImGuiNative.igGetCursorPosY();
            Theme.TinyTriangle(cpos - 15, cposY + 15);
            ImGuiNative.igSetCursorPosX(cpos - w - 13);
            ImGuiNative.igSetCursorPosY(cposY + 2);
            Theme.Icon(options[selected].Icon, Color4.White);
            ImGui.SameLine();
            ImGuiNative.igSetCursorPosY(cposY);
            ImGui.SetCursorPosX(cpos - 6);
            ImGui.Dummy(Vector2.Zero);
            if (clicked)
                ImGui.OpenPopup(id + "#popup");
            if(ImGui.BeginPopup(id + "#popup"))
            {
                ImGui.MenuItem(id, false);
                for (int i = 0; i < options.Count; i++)
                {
                    var opt = options[i];
                    if(Theme.IconMenuItem(opt.Name, opt.Icon, Color4.White, true))
                        selected = i;
                }
                ImGui.EndPopup();
            }
            ImGui.PopID();
        }
    }
}