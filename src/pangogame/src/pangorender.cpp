// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#include "pg_internal.h"
#include "stb.h"


struct CacheRenderer {
	PangoRenderer parent;
	PGRenderContext *ctx;
	PGBuiltText *built;
	PangoAttrType shadowType;
    int colorset;
    float red, green, blue;
    float alpha;
    
    int shadowEnabled;
    float shadowRed;
    float shadowGreen;
    float shadowBlue;
};

struct CacheRendererClass {
	PangoRendererClass class_instance;
};

G_DEFINE_TYPE(CacheRenderer, cacherenderer, PANGO_TYPE_RENDERER);
#define TYPE_CACHERENDERER (cacherenderer_get_type())
#define CACHERENDERER(obj) (G_TYPE_CHECK_INSTANCE_CAST((obj), TYPE_CACHERENDERER, CacheRenderer))

void doDrawRectangle(PangoRenderer* renderer, PangoRenderPart part, int x, int y, int width, int height)
{
	CacheRenderer* ren = CACHERENDERER(renderer);
    float red, green, blue;
    if(ren->colorset) {
        red = ren->red;
        green = ren->green;
        blue = ren->blue;
    } else {
        PangoColor* fg = pango_renderer_get_color(renderer, part);
        red = !fg ? 0.0f : fg->red / 65536.0f;
        green = !fg ? 0.0f : fg->green / 65536.0f;
        blue = !fg ? 0.0f : fg->blue / 65536.0f;
    }
	//shadow quad
    if(part != PANGO_RENDER_PART_BACKGROUND && ren->shadowEnabled) {
        PGQuad shQ;
        shQ.tex = NULL;
        shQ.dstX = PANGO_PIXELS(x) + 2;
        shQ.dstY = PANGO_PIXELS(y) + 2;
        shQ.dstW = PANGO_PIXELS(width);
        shQ.dstH = PANGO_PIXELS(height);
        shQ.r = ren->shadowRed;
        shQ.g = ren->shadowGreen;
        shQ.b = ren->shadowBlue;
        shQ.a = ren->alpha;
        stb_arr_push(ren->built->quads, shQ);
    }
    //color quad
	PGQuad q;
    q.tex = NULL;
	q.dstX = PANGO_PIXELS(x);
	q.dstY = PANGO_PIXELS(y);
	q.dstW = PANGO_PIXELS(width);
	q.dstH = PANGO_PIXELS(height);
	q.r = red;
	q.g = green;
	q.b = blue;
	q.a = ren->alpha;
	stb_arr_push(ren->built->quads, q);
}

// We compile on systems that don't
// have Pango 1.50 available
struct _GlyphVisAttr_150
{
  guint is_cluster_start : 1;
  guint is_color         : 1;
};



void drawGlyphRun (int shadow, uint32_t fontHash, float red, float green, float blue, PangoRenderer* renderer, PangoFont* font, PangoGlyphString* glyphs, int px, int py)
{
    CacheRenderer* ren = CACHERENDERER(renderer);
	
	PangoGlyphUnit layoutX = px;
	PangoGlyphUnit layoutY = py;

	for(int i = 0; i < glyphs->num_glyphs; i++) {
		PangoGlyphInfo* gi = &glyphs->glyphs[i];

		if(gi->glyph & PANGO_GLYPH_UNKNOWN_FLAG) {
			//TODO: figure out how to draw a question mark
			continue;
		}
        
        int isColor = 0;
        if(!shadow && pango_version() >= PANGO_VERSION_ENCODE(1,50,0)) {
            _GlyphVisAttr_150 *visAttr = (_GlyphVisAttr_150*)&gi->attr;
            isColor = visAttr->is_color;
        }
        
		unsigned int glyph = gi->glyph;
		PangoRectangle r;
		pango_font_get_glyph_extents(font, glyph, &r, 0);
		pango_extents_to_pixels(&r, 0);
		float w = r.width;
		float h = r.height;
		if(w <= 0.f && h ,+ 0.f) {
			//Space
			layoutX += gi->geometry.width;
			continue;
		}
		CachedGlyph cached;
		pg_getglyph(ren->ctx, &cached, isColor ? (glyph | GLYPH_COLOR_FLAG) : glyph, fontHash, font);
		PGQuad q;
        q.tex = cached.tex;
		q.srcX = cached.srcX;
		q.srcY = cached.srcY;
		q.srcW = cached.srcW;
		q.srcH = cached.srcH;
		q.dstX = PANGO_PIXELS(layoutX + gi->geometry.x_offset) + cached.offsetLeft;
		q.dstY = PANGO_PIXELS(layoutY + gi->geometry.y_offset) - cached.offsetTop;
		q.dstW = cached.srcW;
		q.dstH = cached.srcH;
		if(isColor) {
		    q.r = 1;
		    q.g = 1;
		    q.b = 1;
		} else {
		    q.r = red;
		    q.g = green;
		    q.b = blue;
		}
		q.a = ren->alpha;
		stb_arr_push(ren->built->quads, q);
		layoutX += gi->geometry.width;
	}
}

void doDrawGlyphs(PangoRenderer* renderer, PangoFont* font, PangoGlyphString* glyphs, int px, int py)
{
	CacheRenderer* ren = CACHERENDERER(renderer);
	float red, green, blue;
    if(ren->colorset) {
        red = ren->red;
        green = ren->green;
        blue = ren->blue;
    } else {
        PangoColor* fg = pango_renderer_get_color(renderer, PANGO_RENDER_PART_FOREGROUND);
        red = !fg ? 0.0f : fg->red / 65536.0f;
        green = !fg ? 0.0f : fg->green / 65536.0f;
        blue = !fg ? 0.0f : fg->blue / 65536.0f;
    }
    
	PangoFontDescription* desc = pango_font_describe(font);
	uint32_t fontHash = (uint32_t)pango_font_description_hash(desc);
	pango_font_description_free(desc);
    if(ren->shadowEnabled) {
        drawGlyphRun(1, fontHash, ren->shadowRed, ren->shadowGreen, ren->shadowBlue, renderer, font, glyphs, px + 2 * PANGO_SCALE, py + (2 * PANGO_SCALE));
    }
    drawGlyphRun(0, fontHash, red, green, blue, renderer, font, glyphs, px, py);
}


static GObjectClass* _pangoClass = 0;

void cacherenderer_init(CacheRenderer* object) //Needed for GObject
{

}

void cacherenderer_finalize(GObject* object)
{
	G_OBJECT_CLASS(_pangoClass)->finalize(object);
}

void doPrepareRun (PangoRenderer  *renderer,
                   PangoLayoutRun *run)
{
    CacheRenderer* ren = CACHERENDERER(renderer);
    PangoAttrType shadowType = pg_getshadowtype(ren->ctx);
    PANGO_RENDERER_CLASS(cacherenderer_parent_class)->prepare_run(renderer, run);
    GSList *l;
    ren->shadowEnabled = 0;
    
    for (l = run->item->analysis.extra_attrs; l; l = l->next)
    {
        PangoAttribute *attr = (PangoAttribute*)l->data;
        if(attr->klass->type == shadowType) {
            PangoColor* color = &((PangoAttrColor *)attr)->color;
            ren->shadowEnabled = 1;
            ren->shadowRed = color->red / 65536.0f;
            ren->shadowGreen = color->green / 65536.0f;
            ren->shadowBlue = color->blue / 65536.0f;
            break;
        }
    }
}

void cacherenderer_class_init(CacheRendererClass* klass)
{
	GObjectClass* object_class = G_OBJECT_CLASS(klass);
	PangoRendererClass* renderer_class = PANGO_RENDERER_CLASS(klass);
	_pangoClass = (GObjectClass*)(g_type_class_peek_parent(klass));
	object_class->finalize = cacherenderer_finalize;
	renderer_class->draw_glyphs = &doDrawGlyphs;
	renderer_class->draw_rectangle = &doDrawRectangle;
	renderer_class->prepare_run = &doPrepareRun;
}

PGBuiltText *pg_pango_constructtext(PGRenderContext *ctx, PangoLayout **layouts, int layoutCount) 
{
	PGBuiltText *built = (PGBuiltText*)malloc(sizeof(PGBuiltText));
	built->layouts = layouts;
	built->layoutCount = layoutCount;
	built->ctx = ctx;
	built->quads = NULL;
    built->initialLen = 0;
	pg_pango_calculatetext(built, NULL);
	return built;
}

void pg_pango_calculatetext(PGBuiltText *text, float *color)
{
	CacheRenderer* doRenderer = (CacheRenderer*)g_object_new(TYPE_CACHERENDERER, 0);
	doRenderer->ctx = text->ctx;
	doRenderer->built = text;
    doRenderer->alpha = 1;
    if(color) {
        doRenderer->colorset = 1;
        doRenderer->red = color[0];
        doRenderer->green = color[1];
        doRenderer->blue = color[2];
        doRenderer->alpha = color[3];
    }
    else
        doRenderer->colorset = 0;
	if(text->quads) 
	{
		stb_arr_free(text->quads);
		text->quads = NULL;
	}
	if(text->initialLen > 0)
        stb_arr_setsize(text->quads, text->initialLen);
	int yOffset = 0;
	for(int i = 0; i < text->layoutCount; i++) {
		PangoLayout *layout = text->layouts[i];
		pango_renderer_draw_layout(
			PANGO_RENDERER(doRenderer),
			layout,
			0, yOffset
		);
		PangoRectangle ink;
		PangoRectangle logical;
		pango_layout_get_extents(layout, &ink, &logical);
		yOffset += logical.height;
	}
	text->height = yOffset / PANGO_SCALE;
	text->quadCount = stb_arr_len(text->quads);
	g_object_unref(doRenderer);
}
