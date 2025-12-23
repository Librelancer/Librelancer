// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
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

    public static void CheckboxUndo(string label, EditorUndoBuffer buffer,
        EditorPropertyModification<bool>.Accessor value)
    {
        var v = value();
        if (ImGui.Checkbox(label, ref v))
        {
            buffer.Set(label, value, v);
        }
    }

    public static void InputTextIdUndo(string label,
        EditorUndoBuffer buffer,
        EditorPropertyModification<string>.Accessor value,
        float width = 0.0f,
        float labelWidth = 0.0f)
    {
        ImGui.PushID(label);
        ImGui.AlignTextToFramePadding();
        Label(label);
        ImGui.SameLine(labelWidth);
        if (width != 0.0f)
        {
            ImGui.SetNextItemWidth(width);
        }
        ImGuiExt.InputTextLogged("##input",
            ref value(),
            250,
            (old, updated) => buffer.Set(label, value, old, updated),
            true);
        ImGui.PopID();
    }

    private static int oldInt = 0;

    public static void InputIntUndo(
        string label,
        EditorUndoBuffer buffer,
        EditorPropertyModification<int>.Accessor value,
        int step = 1,
        int step_fast = 100,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None,
        Point? clamp = null,
        float labelWidth = -1f
    )
    {
        ImGui.PushID(label);
        ImGui.AlignTextToFramePadding();
        Label(label);
        ImGui.SameLine(labelWidth);
        ref int v = ref value();
        int oldCopy = v;
        ImGui.PushItemWidth(-1);
        ImGui.InputInt("##input", ref v, step, step_fast, flags);
        if (clamp != null)
        {
            v = Math.Clamp(v, clamp.Value.X, clamp.Value.Y);
        }
        ImGui.PopItemWidth();
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
        EditorPropertyModification<float>.Accessor value,
        float v_min,
        float v_max,
        string format = "%.3f",
        ImGuiSliderFlags flags = ImGuiSliderFlags.None)
    {
        ImGui.PushID(label);
        ImGui.AlignTextToFramePadding();
        Label(label);
        ImGui.SameLine();
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

    public static void InputFloatUndo(
        string label,
        EditorUndoBuffer buffer,
        EditorPropertyModification<float>.Accessor value,
        int step = 0,
        int step_fast = 0,
        string format = "%.3f",
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None,
        float labelWidth = 100f,
        float inputWidth = 0f

    )
    {
        ImGui.PushID(label);
        ImGui.AlignTextToFramePadding();
        Label(label);
        ImGui.SameLine(labelWidth);
        ref float v = ref value();
        float oldCopy = v;
        ImGui.PushItemWidth(inputWidth);
        ImGui.InputFloat("##input", ref v, step, step_fast, format, flags);
        if (ImGui.IsItemActivated())
        {
            oldFloat = oldCopy;
        }
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            buffer.Set(label, value, oldFloat, v);
        }
        ImGui.PopItemWidth();
        ImGui.PopID();
    }

    private static Quaternion oldQuaternion;

    public static void InputQuaternionUndo(string label,
        EditorUndoBuffer buffer,
        EditorPropertyModification<Quaternion>.Accessor value,
        float labelWidth = 100f,
        float inputWidth = 0f
        )
    {
        ImGui.PushID(label);
        ImGui.AlignTextToFramePadding();
        Label(label);
        ImGui.SameLine(labelWidth);
        ref Quaternion q = ref value();
        Quaternion oldCopy = q;
        ImGui.PushItemWidth(inputWidth);
        ImGui.InputFloat4("##input", ref Unsafe.As<Quaternion, Vector4>(ref q));
        ImGui.PopItemWidth();
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
        EditorPropertyModification<Vector3>.Accessor value,
        string format = "%.3f",
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None,
        float labelWidth = 100f,
        float inputWidth = 0f
    )
    {
        ImGui.PushID(label);
        ImGui.AlignTextToFramePadding();
        Label(label);
        ImGui.SameLine(labelWidth);
        ref Vector3 v = ref value();
        Vector3 oldCopy = v;
        ImGui.PushItemWidth(inputWidth);
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
        ImGui.PopItemWidth();
    }

    public static void DisabledInputTextId(string label, string value, float width = 0.0f)
    {
        ImGui.BeginDisabled(true);
        InputTextIdDirect(label, ref value, width);
        ImGui.EndDisabled();
    }

    public static bool InputTextIdDirect(string label, ref string value, float width = 0.0f)
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



    public static void InputOptionalVector3Undo(string label,
        EditorUndoBuffer buffer,
        EditorPropertyModification<OptionalArgument<Vector3>>.Accessor accessor)
    {
        ref OptionalArgument<Vector3> value = ref accessor();
        if (!value.Present)
        {
            if (ImGui.Button($"Set {label}"))
            {
                value = Vector3.Zero;
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

            var v = value.Value;
            InputFloat3Undo(label, buffer, () => ref accessor().Value);
            value = v;
        }
    }

    public static void InputOptionalFloatUndo(string label,
        EditorUndoBuffer buffer,
        EditorPropertyModification<OptionalArgument<float>>.Accessor accessor)
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

            var v = value.Value;
            InputFloatUndo(label, buffer, () => ref accessor().Value);
            value = v;
        }
    }

    public static void InputOptionalQuaternionUndo(string label,
        EditorUndoBuffer buffer,
        EditorPropertyModification<OptionalArgument<Quaternion>>.Accessor accessor)
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

            var v = value.Value;
            InputQuaternionUndo(label, buffer, () => ref accessor().Value);
            value = v;
        }
    }

    public static void InputStringList(string id, EditorUndoBuffer undoBuffer, List<string> list, bool rmButtonOnEveryElement = true)
    {
        ImGui.PushID(id);
        ImGui.AlignTextToFramePadding();
        Label(id);

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

    private static void IdsInput(string label, string infocard, int ids, bool showTooltipOnHover)
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

    public static void IdsInputStringUndo(string label, GameDataContext gameData, PopupManager popup,
        EditorUndoBuffer undoBuffer, EditorPropertyModification<int>.Accessor accessor,
        bool showTooltipOnHover = true, float inputWidth = 100f, float labelWidth = 0.0f, float buttonWidth = -1f)
    {
        int ids = accessor();
        var infocard = gameData.Infocards.GetStringResource(ids);

        ImGui.Text(label); ImGui.SameLine(labelWidth);

        ImGui.PushItemWidth(inputWidth);
        IdsInput($"##{label}", infocard, ids, showTooltipOnHover);
        ImGui.PopItemWidth();

        ImGui.SameLine();

        ImGui.PushID($"##{label}_button");
        if (ImGui.Button("Browse Ids", new Vector2(buttonWidth, 0f)))
        {
            popup.OpenPopup(new StringSelection(accessor(), gameData.Infocards,
                n => undoBuffer.Set(label, accessor, n)));
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
