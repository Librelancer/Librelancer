// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#ifndef _CIMGUI_DOCK_H_
#define _CIMGUI_DOCK_H_

#ifdef __cplusplus
extern "C" {
#endif
#ifdef _WIN32
#define IGEXPORT __declspec(dllexport)
#else
#define IGEXPORT
#endif
#include <stddef.h>
#include <stdint.h>
//custom controls
IGEXPORT void *igFontFindGlyph(void *font, unsigned short c);
IGEXPORT bool igExtSplitterV(float thickness, float* size1, float *size2, float min_size1, float min_size2, float splitter_long_axis_size);
IGEXPORT bool igExtSpinner(const char* label, float radius, int thickness, int color);
//font
IGEXPORT bool igBuildFontAtlas(void* atlas);
//memory editor
typedef void* memoryedit_t;

IGEXPORT memoryedit_t igExtMemoryEditInit();
IGEXPORT void igExtMemoryEditDrawContents(memoryedit_t memedit, void *mem_data_void_ptr, size_t mem_size, size_t base_display_addr);
IGEXPORT void igExtMemoryEditFree(memoryedit_t memedit);

//text editor
typedef void *texteditor_t;
typedef enum texteditor_mode {
    TEXTEDITOR_MODE_NORMAL,
    TEXTEDITOR_MODE_LUA
} texteditor_mode_t;
IGEXPORT texteditor_t igExtTextEditorInit();
IGEXPORT const char *igExtTextEditorGetText(texteditor_t textedit);
IGEXPORT void igExtTextEditorSetMode(texteditor_t textedit, texteditor_mode_t mode);
IGEXPORT void igExtTextEditorSetReadOnly(texteditor_t textedit, int readonly);
IGEXPORT void igExtFree(void *mem);
IGEXPORT void igExtTextEditorSetText(texteditor_t textedit, const char *text);
IGEXPORT int igExtTextEditorIsTextChanged(texteditor_t textedit);
IGEXPORT void igExtTextEditorGetCoordinates(texteditor_t textedit, int32_t *x, int32_t *y);
IGEXPORT void igExtTextEditorRender(texteditor_t textedit, const char *id);
IGEXPORT void igExtTextEditorFree(texteditor_t textedit);
#ifdef __cplusplus
}
#endif
#endif
