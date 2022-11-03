// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
IGEXPORT void igFtLoad();
IGEXPORT void igMapGlyph(int glyph, int actual);
#ifdef __cplusplus
}
#endif
#endif
