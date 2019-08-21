// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#include <map>
#include "pg_internal.h"
#include "stb.h"

#define PG_MAX(x,y) (x) < (y) ? y : x

struct _PGRenderContext {
	PangoContext *pangoContext;
	PangoFontMap* fontMap;
	PGDrawCallback drawCb;
	PGAllocateTextureCallback allocCb;
	PGUpdateTextureCallback updateCb;
	int curTex;
	int currentX;
	int currentY;
	int lineMax;
	PGTexture* pages[MAX_TEXTURES];
	std::map<uint64_t,CachedGlyph> glyphs;
}; 

static void pg_newtex(PGRenderContext *ctx)
{
	PGTexture *tex = (PGTexture*)malloc(sizeof(PGTexture));
	ctx->allocCb(tex, PG_TEXTURE_SIZE, PG_TEXTURE_SIZE);
	ctx->currentX = 0;
	ctx->currentY = 0;
	ctx->lineMax = 0;
	ctx->pages[ctx->curTex++] = tex;
}

PGRenderContext *pg_createcontext(
	PGAllocateTextureCallback allocate, 
	PGUpdateTextureCallback update, 
	PGDrawCallback draw
)
{
	PGRenderContext *ctx = (PGRenderContext*)malloc(sizeof(PGRenderContext));
	new (ctx) PGRenderContext();
	ctx->allocCb = allocate;
	ctx->updateCb = update;
	ctx->drawCb = draw;
	ctx->curTex = 0;
	ctx->pangoContext = pango_context_new();
	ctx->fontMap = pango_ft2_font_map_new();

	pango_context_set_font_map(ctx->pangoContext, ctx->fontMap);
	pg_newtex(ctx);
	return ctx;
}

#define GLYPHMAP_KEY(x,y) (((uint64_t)(x) << 32) | ((uint64_t)(y)))

void pg_getglyph(PGRenderContext *ctx, CachedGlyph *outGlyph, uint32_t codePoint, uint32_t pangoFontHash, FT_Face face)
{
	std::map<uint64_t,CachedGlyph>::iterator gres = ctx->glyphs.find(GLYPHMAP_KEY(pangoFontHash,codePoint));
	if(gres != ctx->glyphs.end()) {
		*outGlyph = gres->second;
		return;
	}
	FT_Load_Glyph(face, codePoint, FT_LOAD_TARGET_LIGHT);
	FT_Render_Glyph(face->glyph, FT_RENDER_MODE_NORMAL);
	FT_Bitmap rendered = face->glyph->bitmap;
	if(ctx->currentX + rendered.width > PG_TEXTURE_SIZE) {
		ctx->currentX = 0;
		ctx->currentY += ctx->lineMax;
	}
	if(ctx->currentY + rendered.rows > PG_TEXTURE_SIZE) {
		pg_newtex(ctx);
	}
	ctx->lineMax = PG_MAX(ctx->lineMax, rendered.rows);
	PGTexture *tex = ctx->pages[ctx->curTex - 1];
	ctx->updateCb(tex, rendered.buffer, ctx->currentX, ctx->currentY, rendered.width, rendered.rows);
	//create glyph
	outGlyph->tex = tex;
	outGlyph->srcX = ctx->currentX;
	outGlyph->srcY = ctx->currentY;
	outGlyph->srcW = rendered.width;
	outGlyph->srcH = rendered.rows;
	outGlyph->offsetLeft = face->glyph->bitmap_left;
	outGlyph->offsetTop = face->glyph->bitmap_top;
	ctx->glyphs[GLYPHMAP_KEY(pangoFontHash,codePoint)] = outGlyph[0];
	ctx->currentX += rendered.width;
}

static PangoAlignment convert_alignment(PGAlign alignment)
{
	if(alignment == PGAlign_Left)
		return PANGO_ALIGN_LEFT;
	if(alignment == PGAlign_Right)
		return PANGO_ALIGN_RIGHT;
	return PANGO_ALIGN_CENTER;
}

PGBuiltText *pg_buildtext(PGRenderContext* ctx, char **markups, PGAlign* aligns, int paragraphCount, int width)
{
	PangoLayout **layouts = (PangoLayout**)malloc(sizeof(PangoLayout**) * paragraphCount);
	for(int i = 0; i < paragraphCount; i++) {
		PangoLayout *layout = pango_layout_new(ctx->pangoContext);
		pango_layout_set_markup(layout, markups[i], -1);
		pango_layout_set_width(layout, width * PANGO_SCALE);
		pango_layout_set_alignment(layout, convert_alignment(aligns[i]));
		PangoFontDescription *font = pango_font_description_new();
		pango_font_description_set_family(font, "sans");
		pango_font_description_set_size(font, 12 * PANGO_SCALE);
		pango_layout_set_font_description(layout, font);
		pango_font_description_free(font);
		layouts[i] = layout;
	}
	PGBuiltText *built = pg_pango_constructtext(ctx, layouts, paragraphCount);
	return built;
}

void pg_updatewidth(PGBuiltText *text, int width)
{
	for(int i = 0; i < text->layoutCount; i++) {
		PangoLayout *layout = text->layouts[i];
		pango_layout_set_width(layout, width * PANGO_SCALE);
	}
	pg_pango_calculatetext(text);
}

int pg_getheight(PGBuiltText *text)
{
	return text->height;
}

void pg_drawtext(PGRenderContext* ctx, PGBuiltText *text)
{
	for(int i = 0; i < text->runCount; i++) {
		ctx->drawCb(text->runs[i].quads, text->runs[i].tex, text->runs[i].quadCount);
	}
}

void pg_addttfglobal(const char *filename)
{
	const FcChar8 *file = (const FcChar8 *)filename;
	if(!FcConfigAppFontAddFile(NULL, file))
		printf("font add for %s failed\n");
}

void pg_destroytext(PGBuiltText *text)
{
	for(int i = 0; i < text->runCount; i++)
	{
		stb_arr_free(text->runs[i].quads);
	}
	stb_arr_free(text->runs);
	for(int i = 0; i < text->layoutCount; i++)
	{
		g_object_unref(text->layouts[i]);
	}
	free(text->layouts);
	free(text);
}

void pg_destroycontext(PGRenderContext *ctx)
{
	g_object_unref(ctx->pangoContext);
	g_object_unref(ctx->fontMap);

	for(int i = 0; i < ctx->curTex; i++) {
		free(ctx->pages[i]);
	}
	free(ctx);
}
