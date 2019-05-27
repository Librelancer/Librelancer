// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#include "pg_internal.h"
#include "stb.h"

struct CacheRenderer {
	PangoRenderer parent;
	PGRenderContext *ctx;
	PGBuiltText *built;
};

struct CacheRendererClass {
	PangoRendererClass class_instance;
};

G_DEFINE_TYPE(CacheRenderer, cacherenderer, PANGO_TYPE_RENDERER);
#define TYPE_CACHERENDERER (cacherenderer_get_type())
#define CACHERENDERER(obj) (G_TYPE_CHECK_INSTANCE_CAST((obj), TYPE_CACHERENDERER, CacheRenderer))

void doDrawGlyphs(PangoRenderer* renderer, PangoFont* font, PangoGlyphString* glyphs, int px, int py)
{
	CacheRenderer* ren = CACHERENDERER(renderer);
	
	PangoColor* fg = pango_renderer_get_color(renderer, PANGO_RENDER_PART_FOREGROUND);
	float red = !fg ? 0.0f : fg->red / 65536.0f;
	float green = !fg ? 0.0f : fg->green / 65536.0f;
	float blue = !fg ? 0.0f : fg->blue / 65536.0f;

	PangoFontDescription* desc = pango_font_describe(font);
	uint32_t fontHash = (uint32_t)pango_font_description_hash(desc);
	pango_font_description_free(desc);
	
	FT_Face face = pango_fc_font_lock_face((PangoFcFont*) font);
	
	PangoGlyphUnit layoutX = px;
	PangoGlyphUnit layoutY = py;

	for(int i = 0; i < glyphs->num_glyphs; i++) {
		PangoGlyphInfo* gi = &glyphs->glyphs[i];

		if(gi->glyph & PANGO_GLYPH_UNKNOWN_FLAG) {
			//TODO: figure out how to draw a question mark
			continue;
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
		pg_getglyph(ren->ctx, &cached, glyph, fontHash, face);
		
		PGRun *run = NULL;
		for(int j = 0; j < stb_arr_len(ren->built->runs); j++) {
			if(ren->built->runs[i].tex == cached.tex) {
				run = &ren->built->runs[j];
				break;
			}
		}

		if(run == NULL) {
			PGRun r2;
			r2.tex = cached.tex;
			r2.quads = NULL;
			stb_arr_push(ren->built->runs, r2);
			run = &ren->built->runs[stb_arr_len(ren->built->runs)-1];
		}
		PGQuad q;
		q.srcX = cached.srcX;
		q.srcY = cached.srcY;
		q.srcW = cached.srcW;
		q.srcH = cached.srcH;
		q.dstX = PANGO_PIXELS(layoutX + gi->geometry.x_offset) + cached.offsetLeft;
		q.dstY = PANGO_PIXELS(layoutY + gi->geometry.y_offset) - cached.offsetTop;
		q.dstW = cached.srcW;
		q.dstH = cached.srcH;
		q.r = red;
		q.g = green;
		q.b = blue;
		q.a = 1;
		stb_arr_push(run->quads, q);
		layoutX += gi->geometry.width;
	}
	
	pango_fc_font_unlock_face((PangoFcFont*) font);
}


static GObjectClass* _pangoClass = 0;

void cacherenderer_init(CacheRenderer* object) //Needed for GObject
{

}

void cacherenderer_finalize(GObject* object)
{
	G_OBJECT_CLASS(_pangoClass)->finalize(object);
}

void cacherenderer_class_init(CacheRendererClass* klass)
{
	GObjectClass* object_class = G_OBJECT_CLASS(klass);
	PangoRendererClass* renderer_class = PANGO_RENDERER_CLASS(klass);
	_pangoClass = (GObjectClass*)(g_type_class_peek_parent(klass));
	object_class->finalize = cacherenderer_finalize;
	renderer_class->draw_glyphs = &doDrawGlyphs;
}

PGBuiltText *pg_pango_render(PGRenderContext *ctx, PangoLayout *layout)
{
	CacheRenderer* doRenderer = (CacheRenderer*)g_object_new(TYPE_CACHERENDERER, 0);
	doRenderer->ctx = ctx;
	PGBuiltText *built = (PGBuiltText*)malloc(sizeof(PGBuiltText));
	built->runs = NULL;
	doRenderer->built = built;
	pango_renderer_draw_layout(
		PANGO_RENDERER(doRenderer),
		layout,
		0, 0
	);
	built->runCount = stb_arr_len(built->runs);
	for(int i = 0; i < built->runCount; i++) {
		built->runs[i].quadCount = stb_arr_len(built->runs[i].quads);
	}
	g_object_unref(doRenderer);
	return built;
}
