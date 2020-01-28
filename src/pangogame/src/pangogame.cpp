// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#include <map>
#include <string.h>
#include "pg_internal.h"
#include "stb.h"
#include FT_SYNTHESIS_H
#include <fontconfig/fontconfig.h>

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

void pg_getglyph(PGRenderContext *ctx, CachedGlyph *outGlyph, uint32_t codePoint, uint32_t pangoFontHash, FT_Face face, PangoFont* pango)
{
	//Fetch synthesized props
	GValue patternprop = G_VALUE_INIT;
	g_value_init(&patternprop, G_TYPE_POINTER);
	g_object_get_property(G_OBJECT(pango), "pattern", &patternprop);
	FcPattern *pattern = (FcPattern*)g_value_get_pointer(&patternprop);
	FcBool embolden = false;
	if(FcPatternGetBool(pattern, FC_EMBOLDEN, 0, &embolden) != FcResultMatch)
		embolden = FcFalse;
	//Render Glyph
	std::map<uint64_t,CachedGlyph>::iterator gres = ctx->glyphs.find(GLYPHMAP_KEY(pangoFontHash,codePoint));
	if(gres != ctx->glyphs.end()) {
		*outGlyph = gres->second;
		return;
	}
	FT_Load_Glyph(face, codePoint, FT_LOAD_TARGET_LIGHT);
	if(embolden) {
		FT_GlyphSlot_Embolden(face->glyph);
	}
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
	pg_pango_calculatetext(text, NULL);
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
void pg_drawstring(PGRenderContext* ctx, const char *str, const char* fontName, float fontSize, int indent, int underline, float r, float g, float b, float a)
{
    //Layout
    PangoLayout *layout = pango_layout_new(ctx->pangoContext);
    pango_layout_set_indent(layout, indent * PANGO_SCALE);
    pango_layout_set_text(layout, str, strlen(str));
    PangoFontDescription *font = pango_font_description_new();
    pango_font_description_set_family(font, fontName);
    pango_font_description_set_size(font, (int)(fontSize * PANGO_SCALE));
    pango_layout_set_font_description(layout, font);
    pango_font_description_free(font);
    if(underline) {
        PangoAttrList *attrList = pango_attr_list_new();
        PangoAttribute *attribute = pango_attr_underline_new(PANGO_UNDERLINE_SINGLE);
        pango_attr_list_insert(attrList, attribute);
        pango_layout_set_attributes(layout, attrList);
        pango_attr_list_unref(attrList);
    }
    //Calculate
    PGBuiltText built;
    built.layouts = &layout;
    built.layoutCount = 1;
    built.ctx = ctx;
    built.runs = NULL;
    float color[4] = { r, g, b, a };
    pg_pango_calculatetext(&built, color);
    //Draw
    pg_drawtext(ctx, &built);
    //Free
    for(int i = 0; i < built.runCount; i++)
	{
		stb_arr_free(built.runs[i].quads);
	}
	stb_arr_free(built.runs);
	g_object_unref(layout);
}

void pg_measurestring(PGRenderContext* ctx, const char* str, const char* fontName, float fontSize, float *width, float *height)
{
    PangoLayout *layout = pango_layout_new(ctx->pangoContext);
    pango_layout_set_text(layout, str, strlen(str));
    PangoFontDescription *font = pango_font_description_new();
    pango_font_description_set_family(font, fontName);
    pango_font_description_set_size(font, (int)(fontSize * PANGO_SCALE));
    pango_layout_set_font_description(layout, font);
    pango_font_description_free(font);
    PangoRectangle ink;
	PangoRectangle logical;
	pango_layout_get_extents(layout, &ink, &logical);
    *width = (float)(logical.width / PANGO_SCALE);
    *height = (float)(logical.height / PANGO_SCALE);
    g_object_unref(layout);
}

float pg_lineheight(PGRenderContext* ctx, const char* fontName, float fontSize)
{
    PangoFontDescription *desc = pango_font_description_new();
    pango_font_description_set_family(desc, fontName);
    pango_font_description_set_size(desc, (int)(fontSize * PANGO_SCALE));
    PangoFont *font = pango_context_load_font(ctx->pangoContext, desc);
    pango_font_description_free(desc);
    if(!font) return 0;
#if PANGO_VERSION_CHECK(1,44,0)
if(pango_version() >= PANGO_VERSION_ENCODE(1,44,0)) {    
    PangoFontMetrics *metrics = pango_font_get_metrics(font, NULL);
    float retval = (float)(pango_font_metrics_get_height(metrics) / PANGO_SCALE);
    g_object_unref(font);
} else {
#endif
    FT_Face face = pango_fc_font_lock_face((PangoFcFont*) font);
    float retval = (face->size->metrics.height / 64.0f);
    pango_fc_font_unlock_face((PangoFcFont*) font);
    g_object_unref(font);
    return retval;
#if PANGO_VERSION_CHECK(1,44,0)
}
#endif
}

void pg_addttfglobal(const char *filename)
{
	const FcChar8 *file = (const FcChar8 *)filename;
	if(!FcConfigAppFontAddFile(NULL, file))
		printf("font add for %s failed\n", filename);
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
