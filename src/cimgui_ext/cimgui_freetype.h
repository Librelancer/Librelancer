#ifndef _CIMGUI_FREETYPE_H
#define _CIMGUI_FREETYPE_H
#ifdef __cplusplus
extern "C" {
#endif
#ifdef _WIN32
#define IGEXPORT __declspec(dllexport)
#else
#define IGEXPORT
#endif
IGEXPORT bool igBuildFontAtlas(void* atlas);
#ifdef __cplusplus
}
#endif
#endif
