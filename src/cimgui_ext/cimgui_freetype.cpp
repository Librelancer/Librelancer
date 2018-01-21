#include "imgui.h"
#include "imgui_freetype.h"
#include "cimgui_freetype.h"
#include "ftshim.h"

static bool functionsLoaded = false;

IGEXPORT bool igBuildFontAtlas(void* atlas)
{
	if(!functionsLoaded) {
		igLoadFreetype();
		functionsLoaded = true;
	}
	return ImGuiFreeType::BuildFontAtlas((ImFontAtlas*)atlas);
}

/* DYNAMICALLY LOADED FREETYPE USING FREETYPESHIM */
