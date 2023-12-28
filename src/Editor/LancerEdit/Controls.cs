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
            value ??= "";
            ImGui.InputText(name, ref value, 250, ImGuiInputTextFlags.CallbackCharFilter, callback);
        }

        public static bool SmallButton(string text)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0));
            var retval = ImGui.Button(text);
            ImGui.PopStyleVar(1);
            return retval;
        }

        public static void VisibleButton(string name, ref bool visible)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0));
            ImGui.PushID(name);
            var push = !visible;
            if(push) ImGui.PushStyleColor(ImGuiCol.Text, (uint)Color4.Gray.ToAbgr());
            if (ImGui.Button(Icons.Eye)) visible = !visible;
            if(push) ImGui.PopStyleColor();
            ImGui.PopID();
            ImGui.PopStyleVar(1);
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

        public static void InputFlQuaternion(string label, ref Quaternion value)
        {
            var swizzle = new Vector4(value.X, value.Y, value.Z, value.W);
            ImGui.InputFloat4(label, ref swizzle);
            value = new Quaternion(swizzle.X, swizzle.Y, swizzle.Z, swizzle.W);
        }

        public static void InputStringList(string label, List<string> list)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);

            void AddListControls()
            {
                if (ImGui.Button(Icons.PlusCircle))
                {
                    list.Add("");
                    return;
                }

                ImGui.SameLine();
                ImGui.BeginDisabled(list.Count is 0);
                if (ImGui.Button(Icons.X))
                {
                    list.RemoveAt(list.Count - 1);
                }

                ImGui.EndDisabled();
            }

            if (list.Count is 0)
            {
                AddListControls();
                return;
            }

            for (var index = 0; index < list.Count; index++)
            {
                var str = list[index];
                ImGui.PushID(str);

                ImGui.SetNextItemWidth(150f);
                ImGui.InputText("##ID", ref str, 32);
                list[index] = str;

                ImGui.PopID();

                if (index + 1 != list.Count)
                {
                    continue;
                }

                ImGui.SameLine();
                AddListControls();
            }
        }
    }
}
