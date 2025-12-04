#include "dcimgui_nodefaultargfunctions.h"
#pragma once

#ifdef __cplusplus
extern "C" {
#endif

enum ImGuiSeparatorFlags_
{
    ImGuiSeparatorFlags_None                    = 0,
    ImGuiSeparatorFlags_Horizontal              = 1 << 0,   // Axis default to current layout type, so generally Horizontal unless e.g. in a menu bar
    ImGuiSeparatorFlags_Vertical                = 1 << 1,
    ImGuiSeparatorFlags_SpanAllColumns          = 1 << 2,   // Make separator cover all columns of a legacy Columns() set.
};
typedef int ImGuiSeparatorFlags;

CIMGUI_API void ImFontConfig_Construct(ImFontConfig* self);
CIMGUI_API void ImGuiFreeType_AddTintIcon(ImWchar codepoint, ImWchar icon, ImU32 color);
CIMGUI_API void ImGui_SeparatorEx(ImGuiSeparatorFlags flags, float thickness);
CIMGUI_API char* ImGuiEx_GetOriginalInputTextString();
CIMGUI_API void ImGuiEx_FreeOriginalInputTextString(char* str);

#ifdef __cplusplus
}
#endif
