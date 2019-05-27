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
#include <pango/pango-layout.h>
#include <pango/pango-font.h>
#include <pango/pangoft2.h>

#define MAX_TEXTURES 40
#define PG_TEXTURE_SIZE 512

typedef struct {
	PGTexture* tex;
	int srcX, srcY, srcW, srcH;
	int offsetLeft, offsetTop;
} CachedGlyph;

typedef struct {
	PGTexture* tex;
	PGQuad* quads;
	int quadCount;
} PGRun;

struct _PGBuiltText {
	PGRun* runs;
	int runCount;
};

void pg_getglyph(PGRenderContext *ctx, CachedGlyph *outGlyph, uint32_t codePoint, uint32_t pangoFontHash, FT_Face face);
PGBuiltText *pg_pango_render(PGRenderContext *ctx, PangoLayout *layout);

#endif 
