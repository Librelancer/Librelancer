// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Media;

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
    public static class Controls
    {
        public static void InputTextId(string name, ref string value)
        {
            ImGui.InputText(name, ref value, 250, ImGuiInputTextFlags.CallbackCharFilter, callback);
        }
        
        private static unsafe ImGuiInputTextCallback callback = HandleTextEditCallback;
        static unsafe int HandleTextEditCallback(ImGuiInputTextCallbackData* data)
        {
            var ch = (char) data->EventChar;
            if ((ch >= '0' && ch <= '9') ||
                (ch >= 'a' && ch <= 'z') ||
                (ch >= 'A' && ch <= 'Z') ||
                ch == '_')
            {
                return 0;
            }
            if (ch == ' ')
            {
                data->EventChar = (byte)'_';
                return 0;
            }
            return 1;
        }
        
        public static bool Music(string id, MainWindow win, bool enabled = true)
        {
            if (win.Audio.Music.State == PlayState.Playing)
            {
                if (ImGui.Button($"{Icons.Stop}##{id}"))
                    win.Audio.Music.Stop(0);
                return false;
            }
            return ImGuiExt.Button($"{Icons.Play}##{id}", enabled);
        }
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
        
        private static readonly string[] columnNames = new string[] {"A", "B", "C", "D", "E", "F", "G", "H"};
        public static void BeginPropertyTable(string name, params bool[] columns)
        {
            ImGui.BeginTable(name, columns.Length, ImGuiTableFlags.Borders);
            for (int i = 0; i < columns.Length; i++) {
                ImGui.TableSetupColumn(columnNames[i], columns[i] ? ImGuiTableColumnFlags.WidthFixed : ImGuiTableColumnFlags.WidthStretch);   
            }
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
        }

        public static void TruncText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return;
            var fL = text.IndexOf('\n');
            if (fL != -1 || text.Length > maxLength)
            {
                var x = fL != -1
                    ? Math.Min(fL, maxLength)
                    : maxLength;
                var s = ImGuiExt.IDWithExtra(text.Substring(0, x) + "...", "elpt");
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, Vector4.Zero);
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, Vector4.Zero);
                ImGui.Selectable(s, false);
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(text);
            }
            else
            {
                ImGui.TextUnformatted(text);
            }
        }
        
        
        public static void PropertyRow(string name, string value)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(name);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(value);
            ImGui.TableNextColumn();
        }

        public static void EndPropertyTable()
        {
            ImGui.PopStyleVar();
            ImGui.EndTable();
        }
    }
}