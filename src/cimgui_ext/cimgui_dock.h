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
IGEXPORT void *igFontFindGlyph(void *font, unsigned short c);
IGEXPORT bool igExtSplitterV(float thickness, float* size1, float *size2, float min_size1, float min_size2, float splitter_long_axis_size);
#ifdef __cplusplus
}
#endif
#endif
