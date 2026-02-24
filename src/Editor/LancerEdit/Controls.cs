// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using LancerEdit.GameContent.Popups;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.ImUI;
using LibreLancer.Infocards;
using LibreLancer.Sounds;

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

    public static void CheckboxUndo(string label, EditorUndoBuffer buffer,
        FieldAccessor<bool> value)
    {
        if (InEditorTable)
        {
            EditControlSetup(label, 0);
            ImGui.PushID(label);
            var v = value();
            if (ImGui.Checkbox("##value", ref v))
                buffer.Set(label, value, v);
            ImGui.PopID();
        }
        else
        {
            var v = value();
            if (ImGui.Checkbox(label, ref v))
                buffer.Set(label, value, v);
        }
    }

    public static void EditControlSetup(string label, float width, float tableWidth = -1)
    {
        if (InEditorTable)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
        }
        ImGui.AlignTextToFramePadding();
        Label(label);
        if (InEditorTable)
        {
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(tableWidth);
        }
        else
        {
            ImGui.SameLine();
            if (width > 0.0f)
            {
                ImGui.SetNextItemWidth(width);
            }
        }
    }

    public static void InputTextUndo(string label,
        EditorUndoBuffer buffer,
        FieldAccessor<string> value,
        float width = 0.0f)
    {
        ImGui.PushID(label);
        EditControlSetup(label, width);
        ImGuiExt.InputTextLogged("##input",
            ref value(),
            250,
            (old, updated) => buffer.Set(label, value, old, updated),
            false);
        ImGui.PopID();
    }


    public static void InputTextIdUndo(string label,
        EditorUndoBuffer buffer,
        FieldAccessor<string> value,
        float width = 0.0f)
    {
        ImGui.PushID(label);
        EditControlSetup(label, width);
        ImGuiExt.InputTextLogged("##input",
            ref value(),
            250,
            (old, updated) => buffer.Set(label, value, old, updated),
            true);
        ImGui.PopID();
    }

    public static void InputItemNickname<T>(string label,
        EditorUndoBuffer buffer,
        T value,
        Func<string, T, bool> nicknameTaken,
        Func<T, string, string, EditorAction> commit,
        float width = 0.0f) where T : NicknameItem
    {
        ImGui.PushID(label);
        EditControlSetup(label, width);
        if (ImGuiExt.InputTextLogged("##input",
                ref value.Nickname,
                250,
                OnChanged,
                true))
        {
            if (string.IsNullOrEmpty(value.Nickname))
            {
                ImGui.TextColored(Color4.Red, "Nickname cannot be empty");
            }
            else if (nicknameTaken(value.Nickname, value))
            {
                ImGui.TextColored(Color4.Red, $"Item '{value.Nickname}' already exists");
            }
        }

        void OnChanged(string old, string updated)
        {
            if (string.IsNullOrWhiteSpace(updated) || nicknameTaken(value.Nickname, value))
            {
                value.Nickname = old; // Unable to set. Reset
            }
            else
            {
                buffer.Commit(commit(value, old, updated));
            }
        }

        ImGui.PopID();
    }

    public static void InputItemNickname<T>(string label,
        EditorUndoBuffer buffer,
        SortedDictionary<string, T> list,
        T value,
        float width = 0.0f) where T : NicknameItem
    {
        InputItemNickname(label, buffer,
            value,
            (nickname, v) => list.TryGetValue(nickname, out var o) && o != v,
            (v, old, updated) => new ItemRename<T>(old, updated, list, v),
            width);
    }

    private static int oldInt = 0;

    public static void InputIntUndo(
        string label,
        EditorUndoBuffer buffer,
        FieldAccessor<int> value,
        int step = 1,
        int step_fast = 100,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None,
        Point? clamp = null
    )
    {
        ImGui.PushID(label);
        EditControlSetup(label, 0);
        ref int v = ref value();
        int oldCopy = v;
        ImGui.InputInt("##input", ref v, step, step_fast, flags);
        if (clamp != null)
        {
            v = Math.Clamp(v, clamp.Value.X, clamp.Value.Y);
        }
        if (ImGui.IsItemActivated())
        {
            oldInt = oldCopy;
        }
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            buffer.Set(label, value, oldInt, v);
        }
        ImGui.PopID();
    }


    private static float oldFloat = 0f;
    public static void SliderFloatUndo(
        string label,
        EditorUndoBuffer buffer,
        FieldAccessor<float> value,
        float v_min,
        float v_max,
        string format = "%.3f",
        ImGuiSliderFlags flags = ImGuiSliderFlags.None)
    {
        ImGui.PushID(label);
        EditControlSetup(label, 0);
        ref float v = ref value();
        float oldCopy = v;
        ImGui.SliderFloat(label, ref v, v_min, v_max, format, flags);
        if (ImGui.IsItemActivated())
        {
            oldFloat = oldCopy; // Can be modified in the same frame
        }
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            buffer.Set(label, value, oldFloat, v);
        }
        ImGui.PopID();
    }

    public static void InputFloatValueUndo(
        string id,
        EditorUndoBuffer buffer,
        FieldAccessor<float> value,
        Action hook = null,
        string format = "%.3f",
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None
    )
    {
        ImGui.PushID(id);
        ref float v = ref value();
        float oldCopy = v;
        ImGui.InputFloat("##input", ref v, 0, 0, format, flags);
        if (ImGui.IsItemActivated())
        {
            oldFloat = oldCopy;
        }
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            buffer.Set(id, value, oldFloat, v, hook);
        }
        ImGui.PopID();
    }


    public static void InputFloatUndo(
        string label,
        EditorUndoBuffer buffer,
        FieldAccessor<float> value,
        Action hook = null,
        string format = "%.3f",
        float nonTableWidth = 0
    )
    {
        ImGui.PushID(label);
        EditControlSetup(label, nonTableWidth);
        ref float v = ref value();
        float oldCopy = v;
        ImGui.InputFloat("##input", ref v, 0, 0, format);
        if (ImGui.IsItemActivated())
        {
            oldFloat = oldCopy;
        }
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            buffer.Set(label, value, oldFloat, v, hook);
        }
        ImGui.PopID();
    }

    private static Quaternion oldQuaternion;

    public static void InputQuaternionUndo(string label,
        EditorUndoBuffer buffer,
        FieldAccessor<Quaternion> value)
    {
        ImGui.PushID(label);
        EditControlSetup(label, 0);
        ref Quaternion q = ref value();
        Quaternion oldCopy = q;
        ImGui.InputFloat4("##input", ref Unsafe.As<Quaternion, Vector4>(ref q));
        if (ImGui.IsItemActivated())
        {
            oldQuaternion = oldCopy;
        }
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            buffer.Set(label, value, oldQuaternion, q);
        }
        ImGui.PopID();
    }


    private static Vector3 oldVector;
    public static void InputFloat3Undo(
        string label,
        EditorUndoBuffer buffer,
        FieldAccessor<Vector3> value,
        string format = "%.3f",
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None
    )
    {
        ImGui.PushID(label);
        EditControlSetup(label, 0);
        ref Vector3 v = ref value();
        Vector3 oldCopy = v;
        ImGui.InputFloat3("##input", ref v, format, flags);
        if (ImGui.IsItemActivated())
        {
            oldVector = oldCopy;
        }
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            buffer.Set(label, value, oldVector, v);
        }
        ImGui.PopID();
    }

    public static void DisabledInputTextId(string label, string value, float width = 0.0f)
    {
        EditControlSetup(label, width);
        ImGui.PushID(label);
        value ??= "";
        ImGui.BeginDisabled();
        ImGui.InputText("##input", ref value, 250, ImGuiInputTextFlags.ReadOnly);
        ImGui.EndDisabled();
        ImGui.PopID();
    }

    public static bool InputTextIdDirect(string label, ref string value, float width = 0.0f)
    {
        EditControlSetup(label, width);
        ImGui.PushID(label);
        value ??= "";
        var ret = ImGui.InputText("##input", ref value, 250,
            ImGuiInputTextFlags.CallbackCharFilter | ImGuiInputTextFlags.EnterReturnsTrue, callback);
        ImGui.PopID();
        return ret;
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

    public static bool Music(string id, SoundManager sounds, bool enabled = true)
    {
        if (sounds.MusicPlaying)
        {
            if (ImGui.Button($"{Icons.Stop}##{id}"))
                sounds.StopMusic();
            return false;
        }
        return ImGuiExt.Button($"{Icons.Play}##{id}", enabled);
    }


    public static bool GradientButton(string id, Color4 colA, Color4 colB, Vector2 size, bool gradient)
    {
        if (!gradient)
            return ImGui.ColorButton(id, colA, ImGuiColorEditFlags.NoAlpha, size);
        var retval = ImGui.InvisibleButton(id, size);
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var dlist = ImGui.GetWindowDrawList();
        ImGuiHelper.DrawVerticalGradient(dlist, min, max, (VertexDiffuse)colA, (VertexDiffuse)colB, EasingTypes.Linear);
        var b = ImGui.IsItemHovered() ? ImGui.GetColorU32(ImGuiCol.ButtonHovered) : ImGui.GetColorU32(ImGuiCol.Border);
        dlist.AddRect(min, max, b);
        return retval;
    }


    public static bool InEditorTable;
    public static bool BeginEditorTable(string id)
    {
        ImGui.PushID(id);
        if (!ImGui.BeginTable("##editortable", 2))
        {
            ImGui.PopID();
            return false;
        }
        ImGui.TableSetupColumn("##labels", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("##values", ImGuiTableColumnFlags.WidthStretch);
        InEditorTable = true;
        return true;
    }

    public static void TableSeparator()
    {
        ImGui.EndTable();
        ImGui.Separator();
        ImGui.BeginTable("##editortable", 2);
        ImGui.TableSetupColumn("##labels", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("##values", ImGuiTableColumnFlags.WidthStretch);
    }

    [DllImport("cimgui")]
    static extern void igTableFullRowBegin();
    [DllImport("cimgui")]
    static extern void igTableFullRowEnd();

    public static void TableSeparatorText(string heading)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.SeparatorTextBorderSize, 1);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        igTableFullRowBegin();
        ImGui.SeparatorText(heading);
        igTableFullRowEnd();
        ImGui.PopStyleVar();
    }

    public static bool EditButtonRow(string name, string value)
    {
        ImGui.PushID(name);
        EditControlSetup(name, 0, -ButtonWidth($"{Icons.Edit}"));
        ImGui.LabelText("", value);
        ImGui.SameLine();
        var r = ImGui.Button($"{Icons.Edit}");
        ImGui.PopID();
        return r;
    }

    public static void EndEditorTable()
    {
        InEditorTable = false;
        ImGui.EndTable();
        ImGui.PopID();
    }



    public static void InputOptionalVector3Undo(string label,
        EditorUndoBuffer buffer,
        FieldAccessor<OptionalArgument<Vector3>> accessor)
    {
        ref OptionalArgument<Vector3> value = ref accessor();
        if (!value.Present)
        {
            if (ImGui.Button($"Set {label}"))
            {
                buffer.Set(label, accessor, Vector3.Zero);
            }
        }
        else
        {
            if (ImGui.Button($"Clear##{label}"))
            {
                buffer.Set(label, accessor, default);
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (!value.Present) return;

            InputFloat3Undo(label, buffer, () => ref accessor().Value);
        }
    }

    public static void InputOptionalFloatUndo(string label,
        EditorUndoBuffer buffer,
        FieldAccessor<OptionalArgument<float>> accessor)
    {
        ref OptionalArgument<float> value = ref accessor();
        if (!value.Present)
        {
            if (ImGui.Button($"Set {label}"))
            {
                buffer.Set(label, accessor, 0);
            }
        }
        else
        {
            if (ImGui.Button($"Clear##{label}"))
            {
                buffer.Set(label, accessor, default);
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (!value.Present) return;

            InputFloatUndo(label, buffer, () => ref accessor().Value);
        }
    }

    public static void InputOptionalQuaternionUndo(string label,
        EditorUndoBuffer buffer,
        FieldAccessor<OptionalArgument<Quaternion>> accessor)
    {
        ref OptionalArgument<Quaternion> value = ref accessor();
        if (!value.Present)
        {
            if (ImGui.Button($"Set {label}"))
            {
                buffer.Set(label, accessor, Quaternion.Identity);
            }
        }
        else
        {
            if (ImGui.Button($"Clear##{label}"))
            {
                buffer.Set(label, accessor, default);
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (!value.Present) return;

            InputQuaternionUndo(label, buffer, () => ref accessor().Value);
        }
    }

    public static void InputStringList(string id, EditorUndoBuffer undoBuffer, List<string> list, bool rmButtonOnEveryElement = true)
    {
        ImGui.PushID(id);
        EditControlSetup(id,0);
        if (list.Count is 0)
        {
            AddListControls();
            ImGui.PopID();
            return;
        }

        for (var index = 0; index < list.Count; index++)
        {
            var str = list[index];
            ImGui.PushID(index);

            ImGui.SetNextItemWidth(150f);
            int curr = index;
            ImGuiExt.InputTextLogged("###", ref str, 32,
                (old, upd) =>
               undoBuffer.Commit(new ListSet<string>(id, list, curr, old, upd)));
            list[index] = str;

            if (index + 1 != list.Count)
            {
                if (rmButtonOnEveryElement)
                {
                    ImGui.SameLine();
                    if (ImGui.Button(Icons.X))
                    {
                        undoBuffer.Commit(new ListRemove<string>(id, list, index, list[index]));
                    }
                }

                ImGui.PopID();
                continue;
            }

            ImGui.PopID();

            AddListControls();
        }

        ImGui.PopID();
        return;

        void AddListControls()
        {
            ImGui.SameLine();
            if (ImGui.Button(Icons.PlusCircle))
            {
                undoBuffer.Commit(new ListAdd<string>(id, list, ""));
                return;
            }

            ImGui.SameLine();
            ImGui.BeginDisabled(list.Count is 0);
            if (ImGui.Button(Icons.X))
            {
                undoBuffer.Commit(new ListRemove<string>(id, list, list.Count - 1, list[^1]));
            }

            ImGui.EndDisabled();
        }
    }


    static void IdsInput(string label, string infocard, ref int ids, bool showTooltipOnHover)
    {
        string preview = "";
        if (ids != 0)
        {
            preview = infocard is null
                ? $"{Icons.Warning} {ids}"
                : $"{ids} ({infocard})";
        }
        if (!ImGuiExt.InputIntPreview(label, preview, ref ids))
        {
            if (infocard is { Length: > 0 } && showTooltipOnHover)
            {
                ImGui.SetItemTooltip(infocard);
            }
        }
    }

    public static float ButtonWidth(string text)
    {
        var s = ImGui.GetStyle();
        return 2 * s.FramePadding.X + s.FrameBorderSize +
               s.ItemSpacing.X + ImGui.CalcTextSize(text).X;
    }

    private static int oldIds = 0;

    public static void IdsInputStringUndo(string label, GameDataContext gameData, PopupManager popup,
        EditorUndoBuffer undoBuffer, FieldAccessor<int> accessor,
        bool showTooltipOnHover = true, float inputWidth = 100f)
    {
        ImGui.PushID(label);
        EditControlSetup(label, inputWidth, -ButtonWidth($"{Icons.MagnifyingGlass}"));
        ref int ids = ref accessor();
        int oldCopy = ids;
        var infocard = gameData.Infocards.HasStringResource(ids)
                ? gameData.Infocards.GetStringResource(ids)
                : null;
        IdsInput("##idsinput", infocard, ref ids, showTooltipOnHover);
        if (ImGui.IsItemActivated())
        {
            oldIds = oldCopy;
        }
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            undoBuffer.Set(label, accessor, oldIds, ids);
        }
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.MagnifyingGlass}"))
        {
            popup.OpenPopup(new StringSelection(accessor(), gameData.Infocards,
                n => undoBuffer.Set(label, accessor, n)));
        }
        ImGui.PopID();
    }

    static string lastParsed = null;
    static string xmlPreview = null;
    static string PreviewText(string pa, FontManager fonts)
    {
        if (lastParsed == pa)
            return xmlPreview;
        xmlPreview = RDLParse.Parse(pa, fonts).ExtractText();
        lastParsed = pa;
        return xmlPreview;
    }

    public static bool IdsInputXmlUndo(string label, MainWindow win, GameDataContext gameData, PopupManager popup,
        EditorUndoBuffer undoBuffer, FieldAccessor<int> accessor,
        bool showTooltipOnHover = true, float inputWidth = 100f)
    {
        ImGui.PushID(label);
        EditControlSetup(label, inputWidth, -ButtonWidth($"{Icons.MagnifyingGlass}"));
        ref int ids = ref accessor();
        int oldCopy = ids;
        var infocard = gameData.Infocards.HasXmlResource(ids)
            ? PreviewText(gameData.Infocards.GetXmlResource(ids), gameData.Fonts)
            : null;
        IdsInput("##idsinput", infocard, ref ids, showTooltipOnHover);
        if (ImGui.IsItemActivated())
        {
            oldIds = oldCopy;
        }
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            undoBuffer.Set(label, accessor, oldIds, ids);
            return true;
        }
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.MagnifyingGlass}"))
        {
            popup.OpenPopup(new InfocardSelection(ids, win, gameData.Infocards,
                gameData.Fonts, n => undoBuffer.Set(label, accessor, n)));
        }
        ImGui.PopID();
        return false;
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

    public static void HelpMarker(string tooltip)
    {
        ImGui.PushID(ImGuiHelper.TempId());
        var sz = ImGui.CalcTextSize($"{Icons.QuestionCircle}");
        var cpos = ImGui.GetCursorPos();
        var framePadding = ImGui.GetStyle().FramePadding with { X = 0 };
        // Try and center this offset FontAwesome glyph
        ImGui.SetCursorPos(cpos + framePadding * 0.5f);
        ImGui.Text($"{Icons.QuestionCircle}");
        ImGui.SetCursorPos(cpos);
        ImGui.InvisibleButton("##help", sz + (2 * framePadding));
        ImGui.SetItemTooltip(tooltip);
        ImGui.PopID();
    }
}
