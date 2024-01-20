// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#define IMGUI_DEFINE_MATH_OPERATORS
#include "cimgui_ext.h"
#include "imgui.h"
#include "imgui_internal.h"
extern "C" {
IGEXPORT void *igFontFindGlyph(void *font, uint32_t c)
{
    ImFont *fnt = (ImFont*)font;
    return (void*)fnt->FindGlyph((ImWchar)c);
}
IGEXPORT bool igExtSplitterV(float thickness, float* size1, float *size2, float min_size1, float min_size2, float splitter_long_axis_size)
{
    using namespace ImGui;
    ImGuiContext& g = *GImGui;
    ImGuiWindow* window = g.CurrentWindow;
    ImGuiID id = window->GetID("##Splitter");
    ImRect bb;
    bb.Min = window->DC.CursorPos + ImVec2(0.0f,*size1);
    bb.Max = bb.Min + CalcItemSize(ImVec2(splitter_long_axis_size, thickness), 0.0f, 0.0f);
    return SplitterBehavior(bb, id, ImGuiAxis_Y, size1, size2, min_size1, min_size2, 0.0f);
}

IGEXPORT bool igExtComboButton(const char* idstr, const char* preview_value)
{
    using namespace ImGui;
    ImGuiContext& g = *GImGui;
    ImGuiWindow* window = GetCurrentWindow();

    ImGuiNextWindowDataFlags backup_next_window_data_flags = g.NextWindowData.Flags;
    g.NextWindowData.ClearFlags(); // We behave like Begin() and need to consume those values
    if (window->SkipItems)
        return false;

    const ImGuiStyle& style = g.Style;
    const ImGuiID id = window->GetID(idstr);

    const float arrow_size = GetFrameHeight();
    const ImVec2 label_size = CalcTextSize("A", NULL, true);
    const float w = CalcItemWidth();
    const ImRect bb(window->DC.CursorPos, window->DC.CursorPos + ImVec2(w, label_size.y + style.FramePadding.y * 2.0f));
    const ImRect total_bb(bb.Min, bb.Max);
    ItemSize(total_bb, style.FramePadding.y);
    if (!ItemAdd(total_bb, id, &bb))
        return false;

    // Open on click
    bool hovered, held;
    bool pressed = ButtonBehavior(bb, id, &hovered, &held);

    // Render shape
    const ImU32 frame_col = GetColorU32(hovered ? ImGuiCol_FrameBgHovered : ImGuiCol_FrameBg);
    const float value_x2 = ImMax(bb.Min.x, bb.Max.x - arrow_size);
    RenderNavHighlight(bb, id);
    window->DrawList->AddRectFilled(bb.Min, ImVec2(value_x2, bb.Max.y), frame_col, style.FrameRounding, ImDrawFlags_RoundCornersLeft);
    {
        ImU32 bg_col = GetColorU32(hovered ? ImGuiCol_ButtonHovered : ImGuiCol_Button);
        ImU32 text_col = GetColorU32(ImGuiCol_Text);
        window->DrawList->AddRectFilled(ImVec2(value_x2, bb.Min.y), bb.Max, bg_col, style.FrameRounding, ImDrawFlags_RoundCornersRight);
        if (value_x2 + arrow_size - style.FramePadding.x <= bb.Max.x)
            RenderArrow(window->DrawList, ImVec2(value_x2 + style.FramePadding.y, bb.Min.y + style.FramePadding.y), text_col, ImGuiDir_Down, 1.0f);
    }
    RenderFrameBorder(bb.Min, bb.Max, style.FrameRounding);

    // Render preview and label
    if (preview_value != NULL)
    {
        if (g.LogEnabled)
            LogSetNextTextDecoration("{", "}");
        RenderTextClipped(bb.Min + style.FramePadding, ImVec2(value_x2, bb.Max.y), preview_value, NULL, NULL);
    }

    g.NextWindowData.Flags = backup_next_window_data_flags;
    return pressed;
}

static inline float  igExtTrunc(float f)                                           { return (float)(int)(f); }

//Backporting ImGui::SeparatorTextEx
static void igExtSeparatorTextEx(int id, const char* label, const char* label_end, float extra_w)
{
    using namespace ImGui;
    // style vars not in our version yet
    const ImVec2 SeparatorTextPadding = ImVec2(20.0f,3.f);
    const ImVec2 SeparatorTextAlign      = ImVec2(0.0f,0.5f);
    const float SeparatorTextBorderSize = 3.0f;
    //imgui func
    ImGuiContext& g = *GImGui;
    ImGuiWindow* window = g.CurrentWindow;
    ImGuiStyle& style = g.Style;

    const ImVec2 label_size = CalcTextSize(label, label_end, false);
    const ImVec2 pos = window->DC.CursorPos;
    const ImVec2 padding = SeparatorTextPadding;

    const float separator_thickness = SeparatorTextBorderSize;
    const ImVec2 min_size(label_size.x + extra_w + padding.x * 2.0f, ImMax(label_size.y + padding.y * 2.0f, separator_thickness));
    const ImRect bb(pos, ImVec2(window->WorkRect.Max.x, pos.y + min_size.y));
    const float text_baseline_y = igExtTrunc((bb.GetHeight() - label_size.y) * SeparatorTextAlign.y + 0.99999f); //ImMax(padding.y, ImFloor((style.SeparatorTextSize - label_size.y) * 0.5f));
    ItemSize(min_size, text_baseline_y);
    if (!ItemAdd(bb, id))
        return;

    const float sep1_x1 = pos.x;
    const float sep2_x2 = bb.Max.x;
    const float seps_y = igExtTrunc((bb.Min.y + bb.Max.y) * 0.5f + 0.99999f);

    const float label_avail_w = ImMax(0.0f, sep2_x2 - sep1_x1 - padding.x * 2.0f);
    const ImVec2 label_pos(pos.x + padding.x + ImMax(0.0f, (label_avail_w - label_size.x - extra_w) * SeparatorTextAlign.x), pos.y + text_baseline_y); // FIXME-ALIGN

    // This allows using SameLine() to position something in the 'extra_w'
    window->DC.CursorPosPrevLine.x = label_pos.x + label_size.x;

    const ImU32 separator_col = GetColorU32(ImGuiCol_Separator);
    if (label_size.x > 0.0f)
    {
        const float sep1_x2 = label_pos.x - style.ItemSpacing.x;
        const float sep2_x1 = label_pos.x + label_size.x + extra_w + style.ItemSpacing.x;
        if (sep1_x2 > sep1_x1 && separator_thickness > 0.0f)
            window->DrawList->AddLine(ImVec2(sep1_x1, seps_y), ImVec2(sep1_x2, seps_y), separator_col, separator_thickness);
        if (sep2_x2 > sep2_x1 && separator_thickness > 0.0f)
            window->DrawList->AddLine(ImVec2(sep2_x1, seps_y), ImVec2(sep2_x2, seps_y), separator_col, separator_thickness);
        if (g.LogEnabled)
            LogSetNextTextDecoration("---", NULL);
        RenderTextEllipsis(window->DrawList, label_pos, ImVec2(bb.Max.x, bb.Max.y + style.ItemSpacing.y), bb.Max.x, bb.Max.x, label, label_end, &label_size);
    }
    else
    {
        if (g.LogEnabled)
            LogText("---");
        if (separator_thickness > 0.0f)
            window->DrawList->AddLine(ImVec2(sep1_x1, seps_y), ImVec2(sep2_x2, seps_y), separator_col, separator_thickness);
    }
}

//Backporting ImGui::SeparatorText
IGEXPORT void igExtSeparatorText(const char* label)
{
    using namespace ImGui;
    ImGuiWindow* window = GetCurrentWindow();
    if (window->SkipItems)
        return;

    // The SeparatorText() vs SeparatorTextEx() distinction is designed to be considerate that we may want:
    // - allow separator-text to be draggable items (would require a stable ID + a noticeable highlight)
    // - this high-level entry point to allow formatting? (which in turns may require ID separate from formatted string)
    // - because of this we probably can't turn 'const char* label' into 'const char* fmt, ...'
    // Otherwise, we can decide that users wanting to drag this would layout a dedicated drag-item,
    // and then we can turn this into a format function.
    igExtSeparatorTextEx(0, label, FindRenderedTextEnd(label), 0.0f);
}

}
