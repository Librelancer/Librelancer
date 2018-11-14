// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#define IMGUI_DEFINE_MATH_OPERATORS
#include "cimgui_dock.h"
#include "imgui.h"
#include "imgui_internal.h"
IGEXPORT void *igFontFindGlyph(void *font, unsigned short c)
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
