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
IGEXPORT void *igFontFindGlyph(void *font, unsigned short c);
#ifdef __cplusplus
}
#endif
#endif
