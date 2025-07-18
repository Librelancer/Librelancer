#include "imgui.h"
#include "imgui_freetype.h"
namespace cimgui
{
#include "dcimgui_manual.h"
}

CIMGUI_API void cimgui::ImFontConfig_Construct(cimgui::ImFontConfig* self)
{
    IM_PLACEMENT_NEW(reinterpret_cast<::ImFontConfig*>(self)) ::ImFontConfig();
}

CIMGUI_API void cimgui::ImGuiFreeType_AddTintIcon(ImWchar codepoint, ImWchar icon, ImU32 color)
{
    ImGuiFreeType::AddTintIcon(codepoint, icon, color);
}