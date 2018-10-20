// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#include "cimgui_spinner.h"
#include "imgui.h"
#include "imgui_internal.h"
#include <cmath>

IGEXPORT bool igExtSpinner(const char* label, float radius, int thickness, int color) {
	using namespace ImGui;
    ImGuiWindow* window = GetCurrentWindow();
    if (window->SkipItems)
        return false;
        
    ImGuiContext& g = *GImGui;
    const ImGuiStyle& style = g.Style;
    const ImGuiID id = window->GetID(label);
        
    ImVec2 pos = window->DC.CursorPos;
    ImVec2 size((radius )*2, (radius + style.FramePadding.y)*2);
        
    const ImRect bb(pos, ImVec2(pos.x + size.x, pos.y + size.y));
    ItemSize(bb, style.FramePadding.y);
    if (!ItemAdd(bb, id))
        return false;
        
    // Render
    window->DrawList->PathClear();
        
    int num_segments = 30;
    int start = std::abs(sin(g.Time*1.8f)*(num_segments-5));
        
    const float a_min = IM_PI*2.0f * ((float)start) / (float)num_segments;
    const float a_max = IM_PI*2.0f * ((float)num_segments-3) / (float)num_segments;

    const ImVec2 centre = ImVec2(pos.x+radius, pos.y+radius+style.FramePadding.y);
        
    for (int i = 0; i < num_segments; i++) {
        const float a = a_min + ((float)i / (float)num_segments) * (a_max - a_min);
        window->DrawList->PathLineTo(ImVec2(centre.x + cos(a+g.Time*8) * radius,
                                            centre.y + sin(a+g.Time*8) * radius));
    }

    window->DrawList->PathStroke(color, false, thickness);
	return true;
}
