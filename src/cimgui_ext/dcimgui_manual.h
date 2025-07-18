#include "dcimgui_nodefaultargfunctions.h"
#pragma once

#ifdef __cplusplus
extern "C" {
#endif

CIMGUI_API void ImFontConfig_Construct(ImFontConfig* self);
CIMGUI_API void ImGuiFreeType_AddTintIcon(ImWchar codepoint, ImWchar icon, ImU32 color);

#ifdef __cplusplus
}
#endif