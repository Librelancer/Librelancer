// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#ifndef _PANGO_GAME_H_
#define _PANGO_GAME_H_
#ifdef __cplusplus
extern "C" {
#endif
    
typedef struct _PGTexturePrivate PGTexturePrivate;

typedef struct _PGTexture {
	void* userdata;
} PGTexture;

/** TYPES **/
typedef struct _PGQuad {
    PGTexture* tex;
	int srcX;
	int srcY;
	int srcW;
	int srcH;
	int dstX;
	int dstY;
	int dstW;
	int dstH;
	float r;
	float g;
	float b;
	float a;
} PGQuad;


typedef enum {
	PGAlign_Left = 0,
	PGAlign_Right = 1,
	PGAlign_Center = 2
} PGAlign;


typedef struct _PGRenderContext PGRenderContext;
typedef struct _PGBuiltText PGBuiltText;

/** CALLBACKS **/
typedef void (*PGDrawCallback)(PGQuad* quads, int count);
/** Implementation must set userdata variable in PGTexture **/
typedef void (*PGAllocateTextureCallback)(PGTexture *texture, int width, int height);
typedef void (*PGUpdateTextureCallback)(PGTexture *texture, void *buffer, int x, int y, int width, int height);

/** FUNCTIONS **/
void pg_addttfglobal(const char *filename);

PGRenderContext *pg_createcontext(
	PGAllocateTextureCallback allocate, 
	PGUpdateTextureCallback update, 
	PGDrawCallback draw
);

PGBuiltText *pg_buildtext(PGRenderContext* ctx, char **markups, PGAlign* aligns, int paragraphCount, int width);
void pg_drawstring(PGRenderContext* ctx, const char *str, const char* fontName, float fontSize, int indent, int underline, float r, float g, float b, float a, float *shadow);
void pg_measurestring(PGRenderContext* ctx, const char* str, const char* fontName, float fontSize, float *width, float *height);
float pg_lineheight(PGRenderContext* ctx, const char* fontName, float fontSize);
void pg_drawtext(PGRenderContext* ctx, PGBuiltText *text);
void pg_updatewidth(PGBuiltText *text, int width);
int pg_getheight(PGBuiltText *text);
void pg_destroytext(PGBuiltText *text);
void pg_destroycontext(PGRenderContext *ctx);

#ifdef __cplusplus
}
#endif
#endif
