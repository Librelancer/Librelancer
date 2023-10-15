// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#ifndef _PG_INTERNAL_H
#define _PG_INTERNAL_H

#define PANGO_ENABLE_BACKEND 1
#include <pangogame.h>
#include <fontconfig/fontconfig.h>
#include <stdlib.h>
#include <stdint.h>
#include <limits.h>
#include <cairo.h>
#include <pango/pango-layout.h>
#include <pango/pango-renderer.h>
#include <pango/pango-font.h>
#include <pango/pango-utils.h>
#include <pango/pangocairo.h>

#define MAX_TEXTURES 16
#define PG_TEXTURE_SIZE 1024
#define GLYPH_COLOR_FLAG (0x80000000)

typedef struct {
	PGTexture* tex;
	int srcX, srcY, srcW, srcH;
	int offsetLeft, offsetTop;
} CachedGlyph;

struct _PGBuiltText {
	PGQuad* quads;
	PGRenderContext* ctx;
	int quadCount;
	PangoLayout **layouts;
	int layoutCount;
	int height;
    int initialLen;
};

void pg_getglyph(PGRenderContext *ctx, CachedGlyph *outGlyph, uint32_t codePoint, uint32_t pangoFontHash, PangoFont *pango);
PGBuiltText *pg_pango_constructtext(PGRenderContext *ctx, PangoLayout **layouts, int layoutCount);
void pg_pango_calculatetext(PGBuiltText *text, float* color);
PangoAttrType pg_getshadowtype(PGRenderContext *ctx);

#endif 
