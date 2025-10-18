// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent;
using LancerEdit.GameContent.Popups;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Media;

namespace LancerEdit;

public delegate char? InputFilter(char ch);

public static class Controls
{
    public static bool Flag<T>(string id, T value, T flag, out bool set) where T : struct, Enum
    {
        set = !value.HasFlag(flag);
        return ImGuiExt.ToggleButton(id, !set);
    }

    public static char? IdFilter(char ch)
    {
        if ((ch >= '0' && ch <= '9') ||
            (ch >= 'a' && ch <= 'z') ||
            (ch >= 'A' && ch <= 'Z') ||
            ch == '_')
        {
            return ch;
        }
        if (ch == ' ')
        {
            return '_';
        }
        return null;
    }

    public static unsafe bool InputTextFilter(string label, ref string value, InputFilter filter, float width = 0.0f)
    {
        if (width != 0.0f)
        {
            ImGui.SetNextItemWidth(width);
        }
        value ??= "";
        bool retval;
        if (filter != null)
        {
            var cb = (ImGuiInputTextCallback)(data =>
            {
                data->EventChar = (ushort)(filter((char)data->EventChar) ?? 0);
                return 0;
            });
            retval= ImGui.InputText(label, ref value, 250,
                ImGuiInputTextFlags.CallbackCharFilter | ImGuiInputTextFlags.EnterReturnsTrue, cb);
            GC.KeepAlive(cb);
        }
        else
        {
            retval = ImGui.InputText(label, ref value, 250, ImGuiInputTextFlags.EnterReturnsTrue);
        }
        return retval;
    }

    public static bool InputTextId(string label, ref string value, float width = 0.0f)
    {
        if (width != 0.0f)
        {
            ImGui.SetNextItemWidth(width);
        }

        value ??= "";
        return ImGui.InputText(label, ref value, 250,
            ImGuiInputTextFlags.CallbackCharFilter | ImGuiInputTextFlags.EnterReturnsTrue, callback);
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
        if (push) ImGui.PushStyleColor(ImGuiCol.Text, (VertexDiffuse)Color4.Gray);
        if (ImGui.Button(Icons.Eye)) visible = !visible;
        if (push) ImGui.PopStyleColor();
        ImGui.PopID();
        ImGui.PopStyleVar(1);
    }

    private static unsafe ImGuiInputTextCallback callback = HandleTextEditCallback;
    static unsafe int HandleTextEditCallback(ImGuiInputTextCallbackData* data)
    {
        var ch = (char)data->EventChar;
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
        var img = ImGuiHelper.RenderGradient(colA, colB);
        var retval = ImGui.ImageButton(id, img, size, new Vector2(0, 1), new Vector2(0, 0));
        return retval;
    }

    private static readonly string[] columnNames = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };
    public static bool BeginPropertyTable(string name, params bool[] columns)
    {
        if (!ImGui.BeginTable(name, columns.Length, ImGuiTableFlags.Borders))
            return false;
        for (int i = 0; i < columns.Length; i++)
        {
            ImGui.TableSetupColumn(columnNames[i], columns[i] ? ImGuiTableColumnFlags.WidthFixed : ImGuiTableColumnFlags.WidthStretch);
        }
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
        return true;
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
            {
                ImGui.SetNextWindowSize(new Vector2(300, 0), ImGuiCond.Always);
                if (ImGui.BeginTooltip())
                {
                    ImGui.TextWrapped(text);
                    ImGui.EndTooltip();
                }
            }

        }
        else
        {
            ImGui.Text(text);
        }
    }

    public static void PropertyRow(string name, string value)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(name);
        ImGui.TableNextColumn();
        ImGui.Text(value);
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

    public static void InputVec3Nullable(string label, ref Vector3? value)
    {
        if (value == null)
        {
            if (ImGui.Button($"Set {label}"))
            {
                value = Vector3.Zero;
            }
        }
        else
        {
            if (value is null) return;

            var v = value.Value;
            ImGui.InputFloat3(label, ref v, "%0f");
            value = v;

            ImGui.SameLine();
            if (ImGui.Button($"Clear##{label}"))
            {
                value = null;
            }
        }
    }

    public static void InputFlQuaternionNullable(string label, ref Quaternion? value)
    {
        if (value == null)
        {
            if (ImGui.Button($"Set {label}"))
            {
                value = Quaternion.Identity;
            }
        }
        else
        {
            if (ImGui.Button($"Clear##{label}"))
            {
                value = null;
            }

            if (value is null) return;

            var v = value.Value;
            InputFlQuaternion(label, ref v);
            value = v;
        }
    }

    public static void InputStringList(string id, List<string> list, bool rmButtonOnEveryElement = true)
    {
        ImGui.PushID(id);
        ImGui.AlignTextToFramePadding();
        Label(id);

        void AddListControls()
        {
            ImGui.SameLine();
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
            ImGui.PushID(index);

            ImGui.SetNextItemWidth(150f);
            ImGui.InputText("###", ref str, 32);
            list[index] = str;

            if (index + 1 != list.Count)
            {

                if (rmButtonOnEveryElement)
                {
                    ImGui.SameLine();
                    if (ImGui.Button(Icons.X))
                    {
                        list.RemoveAt(index);
                    }
                }

                ImGui.PopID();
                continue;
            }
            ImGui.PopID();

            AddListControls();
        }

        ImGui.PopID();
    }

    private static void IdsInput(string label, string infocard, ref int ids, bool showTooltipOnHover)
    {
        ImGui.InputInt(label, ref ids, 0, 0, ImGuiInputTextFlags.ReadOnly);
        if (infocard is null)
        {
            ImGui.SameLine();
            ImGui.Text(Icons.Warning.ToString());
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("This IDS value is invalid and does not point to a known IDS entry.");
            }
        }
        else if (infocard.Length > 0 && showTooltipOnHover && ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);

            ImGui.Text(infocard[0] == '<' ? XmlFormatter.Prettify(infocard) : infocard);

            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    public static void IdsInputString(string label, GameDataContext gameData, PopupManager popup, ref int ids, Action<int> setIds,
        bool showTooltipOnHover = true, float inputWidth = 100f)
    {
        IdsInputString(label, gameData, popup, null, ref ids, setIds, showTooltipOnHover, inputWidth);
    }

    public static void IdsInputString(string label, GameDataContext gameData, PopupManager popup, MainWindow win, ref int ids, Action<int> setIds,
        bool showTooltipOnHover = true, float inputWidth = 100f)
    {
        var infocard = gameData.Infocards.GetStringResource(ids);

        ImGui.PushItemWidth(inputWidth);
        IdsInput(label, infocard, ref ids, showTooltipOnHover);
        ImGui.PopItemWidth();

        ImGui.PushID(label);
        ImGui.SameLine();
        if (ImGui.Button("Browse Ids"))
        {
            if (win != null)
                popup.OpenPopup(IdsSearch.SearchStrings(gameData.Infocards, gameData.Fonts, win, setIds));
            else
                popup.OpenPopup(IdsSearch.SearchStrings(gameData.Infocards, gameData.Fonts, setIds));
        }

        ImGui.PopID();
    }

    public static void IdsInputInfocard(string label, GameDataContext gameData, PopupManager popup, ref int ids, Action<int> setIds,
        bool showTooltipOnHover = true)
    {
        IdsInputInfocard(label, gameData, popup, null, ref ids, setIds, showTooltipOnHover);
    }

    public static void IdsInputInfocard(string label, GameDataContext gameData, PopupManager popup, MainWindow win, ref int ids, Action<int> setIds,
        bool showTooltipOnHover = true)
    {
        var infocard = gameData.Infocards.GetXmlResource(ids);

        IdsInput(label, infocard, ref ids, showTooltipOnHover);

        ImGui.PushID(label);
        ImGui.SameLine();
        if (ImGui.Button("Browse Ids##ID"))
        {
            if (win != null)
                popup.OpenPopup(IdsSearch.SearchInfocards(gameData.Infocards, gameData.Fonts, win, setIds));
            else
                popup.OpenPopup(IdsSearch.SearchInfocards(gameData.Infocards, gameData.Fonts, setIds));
        }
        ImGui.PopID();
    }

    public static void HelpMarker(string helpText, bool sameLine = false)
    {
        if (sameLine)
        {
            ImGui.SameLine();
        }

        ImGui.TextDisabled("(?)");
        if (!ImGui.BeginItemTooltip())
        {
            return;
        }

        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
        ImGui.Text(helpText);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }

    static void Label(string id)
    {
        var i = id.IndexOf("##", StringComparison.Ordinal);
        if(i == -1)
        {
            ImGui.Text(id);
        }
        else
        {
            ImGui.Text(id, i);
        }
    }
}
