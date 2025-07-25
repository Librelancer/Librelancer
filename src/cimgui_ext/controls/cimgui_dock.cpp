// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#define IMGUI_DEFINE_MATH_OPERATORS
#include "cimgui_ext.h"
#include "imgui.h"
#include "imgui_internal.h"
#include <assert.h>

static assertion_fail_handler fail_handler = 0;
void igCSharpAssert(bool expr, const char *exprString, const char *file, int line)
{
    if(fail_handler) {
        if(!expr) {
            fail_handler(exprString, file, line);
        }
    } else {
        assert(expr);
    }
}

CIMGUI_API void igInstallAssertHandler(assertion_fail_handler handler)
{
    fail_handler = handler;
}

CIMGUI_API bool igExtSplitterV(float thickness, float* size1, float *size2, float min_size1, float min_size2, float splitter_long_axis_size)
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

CIMGUI_API void igExtRenderArrow(float frameX, float frameY)
{
    using namespace ImGui;
    ImGuiContext& g = *GImGui;
    const ImGuiStyle& style = g.Style;
    ImU32 text_col = GetColorU32(ImGuiCol_Text);
    ImGuiWindow* window = GetCurrentWindow();
    RenderArrow(window->DrawList, ImVec2(frameX + style.FramePadding.y, frameY + style.FramePadding.y), text_col, ImGuiDir_Down, 1.0f);
}

CIMGUI_API void igExtDrawListAddTriangleMesh(void* drawlist, float* vertices, int32_t count, uint32_t color)
{
    using namespace ImGui;
    ImDrawList *dl = (ImDrawList*)drawlist;
    dl->PrimReserve(count, count);
    ImVec2* points = (ImVec2*)vertices;
    ImDrawIdx idx = (ImDrawIdx)dl->_VtxCurrentIdx;
    for (int i = 0; i < count; i++)
    {
        dl->_IdxWritePtr[i] = (ImDrawIdx)(idx + i);
        dl->_VtxWritePtr[i].pos = points[i];
        dl->_VtxWritePtr[i].uv = dl->_Data->TexUvWhitePixel;
        dl->_VtxWritePtr[i].col = color;
    }
    dl->_VtxWritePtr += count;
    dl->_IdxWritePtr += count;
    dl->_VtxCurrentIdx += count;
}

CIMGUI_API bool igExtComboButton(const char* idstr, const char* preview_value)
{
    using namespace ImGui;
    ImGuiContext& g = *GImGui;
    ImGuiWindow* window = GetCurrentWindow();

    ImGuiNextWindowDataFlags backup_next_window_data_flags = g.NextWindowData.HasFlags;
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
    RenderNavCursor(bb, id);
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

    g.NextWindowData.HasFlags = backup_next_window_data_flags;
    return pressed;
}

CIMGUI_API void igExtUseTitlebar(float *restoreX, float *restoreY)
{
    const ImGuiWindow* window = ImGui::GetCurrentWindow();
    const ImRect titleBarRect = window->TitleBarRect();
    auto cursorPos = ImGui::GetCursorPos();
    *restoreX = cursorPos.x;
    *restoreY = cursorPos.y;
    ImGui::PushClipRect( titleBarRect.Min, titleBarRect.Max, false);
}
