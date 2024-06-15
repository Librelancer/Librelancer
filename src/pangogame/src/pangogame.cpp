// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#include <map>
#include <math.h>
#include <string.h>
#include "pg_internal.h"
#include "stb.h"
#ifndef __APPLE__
#include <fontconfig/fontconfig.h>
#endif

#define PG_MAX(x,y) (x) < (y) ? y : x

struct _PGPacking {
    int curTex;
    int currentX;
    int currentY;
    int lineMax;
    PGTexture* pages[MAX_TEXTURES];
};

struct _PGRenderContext {
	PangoContext *pangoContext;
	PangoFontMap* fontMap;
	PGDrawCallback drawCb;
	PGAllocateTextureCallback allocCb;
	PGUpdateTextureCallback updateCb;
	_PGPacking texa8;
	_PGPacking texargb32;
	std::map<uint64_t,CachedGlyph> glyphs;
	PangoAttrType shadowType;
	PangoAttrClass shadowClass;
};

static void pg_newtex(PGRenderContext *ctx, _PGPacking *pack, int color)
{
	PGTexture *tex = (PGTexture*)malloc(sizeof(PGTexture));
	ctx->allocCb(tex, PG_TEXTURE_SIZE, PG_TEXTURE_SIZE, color);
	pack->currentX = 0;
	pack->currentY = 0;
	pack->lineMax = 0;
	pack->pages[pack->curTex++] = tex;
}

static PangoAttribute * attr_shadow_new(PGRenderContext* ctx, guint16 red,
                           guint16 green,
                           guint16 blue)
{

    PangoAttrColor *result = g_slice_new (PangoAttrColor);
    pango_attribute_init (&result->attr, &ctx->shadowClass);
    result->color.red = red;
    result->color.green = green;
    result->color.blue = blue;

    return (PangoAttribute *)result;
}

PGRenderContext *pg_createcontext(
	PGAllocateTextureCallback allocate,
	PGUpdateTextureCallback update,
	PGDrawCallback draw
)
{
	PGRenderContext *ctx = (PGRenderContext*)malloc(sizeof(PGRenderContext));
	memset(ctx, 0, sizeof(PGRenderContext));
	new (ctx) PGRenderContext();
	ctx->allocCb = allocate;
	ctx->updateCb = update;
	ctx->drawCb = draw;
	ctx->pangoContext = pango_context_new();
	ctx->fontMap = pango_cairo_font_map_new();
	//Define our shadow type, copy vtable from other color.
	ctx->shadowType = pango_attr_type_register("PG_SHADOW_ATTR");
	PangoAttribute *colorAttr = pango_attr_foreground_new(0,0,0);
	ctx->shadowClass = *(colorAttr->klass);
	ctx->shadowClass.type = ctx->shadowType;
	pango_attribute_destroy(colorAttr);


    pango_cairo_font_map_set_resolution(PANGO_CAIRO_FONT_MAP(ctx->fontMap), 72.0);
	pango_context_set_font_map(ctx->pangoContext, ctx->fontMap);
	pg_newtex(ctx, &ctx->texa8, 0);
	pg_newtex(ctx, &ctx->texargb32, 1);

	return ctx;
}

#define GLYPHMAP_KEY(x,y) (((uint64_t)(x) << 32) | ((uint64_t)(y)))

static const uint8_t pg_gamma[0x100] = {
    0x00, 0x0B, 0x11, 0x15, 0x19, 0x1C, 0x1F, 0x22, 0x25, 0x27, 0x2A, 0x2C, 0x2E, 0x30, 0x32, 0x34,
    0x36, 0x38, 0x3A, 0x3C, 0x3D, 0x3F, 0x41, 0x43, 0x44, 0x46, 0x47, 0x49, 0x4A, 0x4C, 0x4D, 0x4F,
    0x50, 0x51, 0x53, 0x54, 0x55, 0x57, 0x58, 0x59, 0x5B, 0x5C, 0x5D, 0x5E, 0x60, 0x61, 0x62, 0x63,
    0x64, 0x65, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75,
    0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F, 0x80, 0x81, 0x82, 0x83, 0x84, 0x84,
    0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8E, 0x8F, 0x90, 0x91, 0x92, 0x93,
    0x94, 0x95, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F, 0x9F, 0xA0,
    0xA1, 0xA2, 0xA3, 0xA3, 0xA4, 0xA5, 0xA6, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAA, 0xAB, 0xAC, 0xAD,
    0xAD, 0xAE, 0xAF, 0xB0, 0xB0, 0xB1, 0xB2, 0xB3, 0xB3, 0xB4, 0xB5, 0xB6, 0xB6, 0xB7, 0xB8, 0xB8,
    0xB9, 0xBA, 0xBB, 0xBB, 0xBC, 0xBD, 0xBD, 0xBE, 0xBF, 0xBF, 0xC0, 0xC1, 0xC2, 0xC2, 0xC3, 0xC4,
    0xC4, 0xC5, 0xC6, 0xC6, 0xC7, 0xC8, 0xC8, 0xC9, 0xCA, 0xCA, 0xCB, 0xCC, 0xCC, 0xCD, 0xCE, 0xCE,
    0xCF, 0xD0, 0xD0, 0xD1, 0xD2, 0xD2, 0xD3, 0xD4, 0xD4, 0xD5, 0xD6, 0xD6, 0xD7, 0xD7, 0xD8, 0xD9,
    0xD9, 0xDA, 0xDB, 0xDB, 0xDC, 0xDC, 0xDD, 0xDE, 0xDE, 0xDF, 0xE0, 0xE0, 0xE1, 0xE1, 0xE2, 0xE3,
    0xE3, 0xE4, 0xE4, 0xE5, 0xE6, 0xE6, 0xE7, 0xE7, 0xE8, 0xE9, 0xE9, 0xEA, 0xEA, 0xEB, 0xEC, 0xEC,
    0xED, 0xED, 0xEE, 0xEF, 0xEF, 0xF0, 0xF0, 0xF1, 0xF1, 0xF2, 0xF3, 0xF3, 0xF4, 0xF4, 0xF5, 0xF5,
    0xF6, 0xF7, 0xF7, 0xF8, 0xF8, 0xF9, 0xF9, 0xFA, 0xFB, 0xFB, 0xFC, 0xFC, 0xFD, 0xFD, 0xFE, 0xFF
};

void pg_getglyph(PGRenderContext *ctx, CachedGlyph *outGlyph, uint32_t codePoint, uint32_t pangoFontHash, PangoFont* pango)
{
	//Render Glyph
	std::map<uint64_t,CachedGlyph>::iterator gres = ctx->glyphs.find(GLYPHMAP_KEY(pangoFontHash,codePoint));
	if(gres != ctx->glyphs.end()) {
		*outGlyph = gres->second;
		return;
	}

	int isColor = (codePoint & GLYPH_COLOR_FLAG);

	PangoRectangle ink_rect;
	pango_font_get_glyph_extents (pango, (codePoint & ~GLYPH_COLOR_FLAG), &ink_rect, NULL);
    pango_extents_to_pixels (&ink_rect, NULL);

    size_t bufferSize = (ink_rect.height * ink_rect.width * (isColor ? 4 : 1));
    unsigned char *buffer = (unsigned char*)malloc(bufferSize);
    memset(buffer, 0, bufferSize);

    cairo_surface_t *surface = cairo_image_surface_create(isColor ? CAIRO_FORMAT_ARGB32 : CAIRO_FORMAT_A8, ink_rect.width, ink_rect.height);
    int stride = cairo_image_surface_get_stride(surface);

    cairo_t *cr = cairo_create(surface);
    cairo_set_source_rgba(cr, 1, 1, 1, 1);
    PangoGlyphString glyph_string;
    PangoGlyphInfo glyph_info;

    glyph_info.glyph = (codePoint & ~GLYPH_COLOR_FLAG);
    glyph_info.geometry.width = (ink_rect.width * 1024);
    glyph_info.geometry.x_offset = -(ink_rect.x * 1024);
    glyph_info.geometry.y_offset = -(ink_rect.y * 1024);
    glyph_string.num_glyphs = 1;
    glyph_string.glyphs = &glyph_info;

    pango_cairo_show_glyph_string(cr, pango, &glyph_string);
    cairo_destroy(cr);
    cairo_surface_flush(surface);
    unsigned char *data = cairo_image_surface_get_data(surface);
    if(isColor) {
        //Straight copy
        memcpy(buffer, data, bufferSize);
    } else {
        //Tightly pack image and gamma correct
        for(int y = 0; y < ink_rect.height; y++) {
            for(int x = 0; x < ink_rect.width; x++) {
                buffer[(y * ink_rect.width) + x] = pg_gamma[data[(y * stride) + x]];
            }
        }
    }
    cairo_surface_destroy(surface);

	//Upload
	_PGPacking *packing = isColor ? &ctx->texargb32 : &ctx->texa8;

	if(packing->currentX + ink_rect.width > PG_TEXTURE_SIZE) {
		packing->currentX = 0;
		packing->currentY += packing->lineMax;
        packing->lineMax = 0;
	}
	if(packing->currentY + ink_rect.height > PG_TEXTURE_SIZE) {
		pg_newtex(ctx, packing, isColor);
	}
	packing->lineMax = PG_MAX(packing->lineMax, ink_rect.height);
	PGTexture *tex = packing->pages[packing->curTex - 1];
	ctx->updateCb(tex, buffer, packing->currentX, packing->currentY, ink_rect.width, ink_rect.height);
	free(buffer);
	//create glyph
	outGlyph->tex = tex;
	outGlyph->srcX = packing->currentX;
	outGlyph->srcY = packing->currentY;
	outGlyph->srcW = ink_rect.width;
	outGlyph->srcH = ink_rect.height;
	outGlyph->offsetLeft = ink_rect.x;
	outGlyph->offsetTop = -ink_rect.y;
	ctx->glyphs[GLYPHMAP_KEY(pangoFontHash,codePoint)] = outGlyph[0];
	packing->currentX += ink_rect.width;
}

static PangoAlignment convert_alignment(PGAlign alignment)
{
	if(alignment == PGAlign_Left)
		return PANGO_ALIGN_LEFT;
	if(alignment == PGAlign_Right)
		return PANGO_ALIGN_RIGHT;
	return PANGO_ALIGN_CENTER;
}

#define PG_8To16(x,y) (guint16)(((x >> y) & 0xFF) * 257)


PGBuiltText *pg_buildtext(PGRenderContext *ctx,
                          PGParagraph     *paragraphs,
                          int              paragraphCount,
                          int              width)
{
	PangoFontDescription *defaultFont = pango_font_description_new();
	pango_font_description_set_family(defaultFont, "sans");
	pango_font_description_set_size(defaultFont, 12 * PANGO_SCALE);

    PangoLayout **layouts = (PangoLayout**)malloc(sizeof(PangoLayout**) * paragraphCount);
	for(int i = 0; i < paragraphCount; i++) {
	    PangoLayout *layout = pango_layout_new(ctx->pangoContext);
	    pango_layout_set_text(layout, paragraphs[i].text, strlen(paragraphs[i].text));
	    pango_layout_set_width(layout, width * PANGO_SCALE);
		pango_layout_set_alignment(layout, convert_alignment(paragraphs[i].alignment));
		pango_layout_set_font_description(layout, defaultFont);
		PangoAttrList *attrList = pango_attr_list_new();
		for(int j = 0; j < paragraphs[i].attributeCount; j++) {
		    if(paragraphs[i].attributes[j].bold) {
		        PangoAttribute* boldAttr = pango_attr_weight_new(PANGO_WEIGHT_BOLD);
		        boldAttr->start_index = paragraphs[i].attributes[j].startIndex;
		        boldAttr->end_index = paragraphs[i].attributes[j].endIndex;
		        pango_attr_list_insert(attrList, boldAttr);
		    }
		    if(paragraphs[i].attributes[j].italic) {
		        PangoAttribute* italicAttr = pango_attr_style_new(PANGO_STYLE_ITALIC);
		        italicAttr->start_index = paragraphs[i].attributes[j].startIndex;
		        italicAttr->end_index = paragraphs[i].attributes[j].endIndex;
		        pango_attr_list_insert(attrList, italicAttr);
		    }
		    if(paragraphs[i].attributes[j].underline) {
		        PangoAttribute *underlineAttr = pango_attr_underline_new(PANGO_UNDERLINE_SINGLE);
		        underlineAttr->start_index = paragraphs[i].attributes[j].startIndex;
		        underlineAttr->end_index = paragraphs[i].attributes[j].endIndex;
                pango_attr_list_insert(attrList, underlineAttr);
		    }
		    if(paragraphs[i].attributes[j].shadowEnabled) {
		        PangoAttribute *shadowAttr = attr_shadow_new(
		            ctx,
                    PG_8To16(paragraphs[i].attributes[j].shadowColor, 0),
                    PG_8To16(paragraphs[i].attributes[j].shadowColor, 8),
                    PG_8To16(paragraphs[i].attributes[j].shadowColor, 16)
                );
                shadowAttr->start_index = paragraphs[i].attributes[j].startIndex;
		        shadowAttr->end_index = paragraphs[i].attributes[j].endIndex;
		        pango_attr_list_insert(attrList, shadowAttr);
		    }
		    if(paragraphs[i].attributes[j].backgroundEnabled) {
		        PangoAttribute *backgroundAttr = pango_attr_background_new(
                    PG_8To16(paragraphs[i].attributes[j].backgroundColor, 0),
                    PG_8To16(paragraphs[i].attributes[j].backgroundColor, 8),
                    PG_8To16(paragraphs[i].attributes[j].backgroundColor, 16)
                );
                backgroundAttr->start_index = paragraphs[i].attributes[j].startIndex;
		        backgroundAttr->end_index = paragraphs[i].attributes[j].endIndex;
		        pango_attr_list_insert(attrList, backgroundAttr);
		    }
		    PangoAttribute *colorAttr = pango_attr_foreground_new(
                PG_8To16(paragraphs[i].attributes[j].fgColor, 0),
                PG_8To16(paragraphs[i].attributes[j].fgColor, 8),
                PG_8To16(paragraphs[i].attributes[j].fgColor, 16)
            );
            colorAttr->start_index = paragraphs[i].attributes[j].startIndex;
		    colorAttr->end_index = paragraphs[i].attributes[j].endIndex;
		    pango_attr_list_insert(attrList, colorAttr);
		    PangoFontDescription *font = pango_font_description_new();
		    pango_font_description_set_family(font, paragraphs[i].attributes[j].fontName);
		    pango_font_description_set_size(font, paragraphs[i].attributes[j].fontSize * PANGO_SCALE);
		    PangoAttribute *fontAttr = pango_attr_font_desc_new(font);
		    fontAttr->start_index = paragraphs[i].attributes[j].startIndex;
		    fontAttr->end_index = paragraphs[i].attributes[j].endIndex;
		    pango_attr_list_insert(attrList, fontAttr);
		    pango_font_description_free(font);
		}
		pango_layout_set_attributes(layout, attrList);
        pango_attr_list_unref(attrList);
        layouts[i] = layout;
	}
	pango_font_description_free(defaultFont);
	PGBuiltText *built = pg_pango_constructtext(ctx, layouts, paragraphCount);
	return built;
}

PGBuiltText *pg_buildtext_markup(PGRenderContext* ctx, char **markups, PGAlign* aligns, int paragraphCount, int width)
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

void pg_get_caret_position(PGBuiltText *text, int paragraph, int textPosition, int *outX, int *outY, int *outW, int *outH)
{
    PangoRectangle result;
    pango_layout_get_cursor_pos(text->layouts[paragraph], textPosition, &result, NULL);
    pango_extents_to_pixels (&result, NULL);
    *outX = result.x;
    *outY = result.y;
    *outW = 2; //no width from pango for caret
    *outH = result.height;
}

void pg_updatewidth(PGBuiltText *text, int width)
{
	for(int i = 0; i < text->layoutCount; i++) {
		PangoLayout *layout = text->layouts[i];
		pango_layout_set_width(layout, width * PANGO_SCALE);
	}
	pg_pango_calculatetext(text, NULL);
}

PangoAttrType pg_getshadowtype(PGRenderContext *ctx)
{
    return ctx->shadowType;
}

int pg_getheight(PGBuiltText *text)
{
	return text->height;
}

void pg_drawtext(PGRenderContext* ctx, PGBuiltText *text)
{
    if(text->quadCount == 0) return;
    ctx->drawCb(text->quads, text->quadCount);
}

#define MulColor(x) ((guint16)((x) * 65535))
void pg_drawstring(PGRenderContext* ctx, const char *str, const char* fontName, float fontSize, PGAlign align, float maxWidth, int underline, float r, float g, float b, float a, float *shadow, float *oWidth, float *oHeight)
{
    //Layout
    PangoLayout *layout = pango_layout_new(ctx->pangoContext);
    pango_layout_set_alignment(layout, convert_alignment(align));
    if(maxWidth > 0) {
        pango_layout_set_width(layout, (int)(maxWidth * PANGO_SCALE));
    }
    pango_layout_set_text(layout, str, strlen(str));
    PangoFontDescription *font = pango_font_description_new();
    pango_font_description_set_family(font, fontName);
    pango_font_description_set_size(font, (int)(fontSize * PANGO_SCALE));
    pango_layout_set_font_description(layout, font);
    pango_font_description_free(font);
    PangoAttrList *attrList = pango_attr_list_new();
    if(underline) {
        PangoAttribute *attribute = pango_attr_underline_new(PANGO_UNDERLINE_SINGLE);
        pango_attr_list_insert(attrList, attribute);
    }
    if(shadow) {
        PangoAttribute *attribute = attr_shadow_new(ctx,
            MulColor(shadow[0]),
            MulColor(shadow[1]),
            MulColor(shadow[2])
        );
        pango_attr_list_insert(attrList, attribute);
    }
    pango_layout_set_attributes(layout, attrList);
    pango_attr_list_unref(attrList);
    PangoRectangle ink;
	PangoRectangle logical;
	pango_layout_get_extents(layout, &ink, &logical);
    if(oWidth)
        *oWidth = (float)(logical.width / PANGO_SCALE);
    if(oHeight)
        *oHeight = (float)(logical.height / PANGO_SCALE);
    //Calculate
    PGBuiltText built;
    built.layouts = &layout;
    built.layoutCount = 1;
    built.ctx = ctx;
    built.quads = NULL;
    built.initialLen = strlen(str);
    float color[4] = { r, g, b, a };
    pg_pango_calculatetext(&built, color);
    //Draw
    pg_drawtext(ctx, &built);
    //Free
	stb_arr_free(built.quads);
	g_object_unref(layout);
}

void pg_measurestring(PGRenderContext* ctx, const char* str, const char* fontName, float fontSize, float maxWidth, float *width, float *height)
{
    PangoLayout *layout = pango_layout_new(ctx->pangoContext);
    if(maxWidth > 0) {
        pango_layout_set_width(layout, (int)(maxWidth * PANGO_SCALE));
    }
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
    PangoFontMetrics *metrics = pango_font_get_metrics(font, NULL);
    //height = ascent + descent (get_height is not available in Ubuntu 20.04)
    float ascent = (float)(pango_font_metrics_get_ascent(metrics) / PANGO_SCALE);
    float descent = (float)(pango_font_metrics_get_descent(metrics) / PANGO_SCALE);
    g_object_unref(font);
    return ascent + descent;
}

void pg_addttfglobal(const char *filename)
{
#ifndef __APPLE__
	const FcChar8 *file = (const FcChar8 *)filename;
	if(!FcConfigAppFontAddFile(NULL, file))
		printf("font add for %s failed\n", filename);
#endif
}

void pg_destroytext(PGBuiltText *text)
{
	stb_arr_free(text->quads);
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

	for(int i = 0; i < ctx->texa8.curTex; i++) {
		free(ctx->texa8.pages[i]);
	}
	for(int i = 0; i < ctx->texargb32.curTex; i++) {
		free(ctx->texargb32.pages[i]);
	}
	free(ctx);
}
