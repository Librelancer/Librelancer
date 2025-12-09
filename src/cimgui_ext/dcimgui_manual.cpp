#include "imgui.h"
#include "imgui_internal.h"
#include "imgui_freetype.h"
namespace cimgui
{
#include "dcimgui_manual.h"
}

#include <cstring>
#include <cstdlib>
#ifdef _MSC_VER
#define strdup _strdup
#endif

CIMGUI_API void cimgui::ImFontConfig_Construct(cimgui::ImFontConfig* self)
{
    IM_PLACEMENT_NEW(reinterpret_cast<::ImFontConfig*>(self)) ::ImFontConfig();
}

CIMGUI_API void cimgui::ImGuiFreeType_AddTintIcon(ImWchar codepoint, ImWchar icon, ImU32 color)
{
    ImGuiFreeType::AddTintIcon(codepoint, icon, color);
}

CIMGUI_API void cimgui::ImGui_SeparatorEx(cimgui::ImGuiSeparatorFlags flags, float thickness)
{
    ImGui::SeparatorEx(static_cast<::ImGuiSeparatorFlags>(flags), thickness);
}

CIMGUI_API char* cimgui::ImGuiEx_GetOriginalInputTextString()
{
    ImGuiInputTextState* state = ImGui::GetInputTextState(ImGui::GetItemID());
    char *newstr = (char*)malloc(state->TextToRevertTo.Size);
    memcpy(newstr, state->TextToRevertTo.Data, state->TextToRevertTo.Size - 1);
    newstr[state->TextToRevertTo.Size - 1] = 0;
    return newstr;
}

CIMGUI_API void cimgui::ImGuiEx_FreeOriginalInputTextString(char* str)
{
    if(str)
        free(str);
}
