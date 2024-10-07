// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#include "imgui.h"
#include "imgui_freetype.h"
#include "cimgui_freetype.h"
static bool functionsLoaded = false;
extern "C" {
IGEXPORT void igFtLoad()
{
}

IGEXPORT void igMapGlyph(int glyph, int actual)
{
    ImGuiFreeType::MapGlyph(glyph, actual);
}
}
/* DYNAMICALLY LOADED FREETYPE USING FREETYPESHIM */
