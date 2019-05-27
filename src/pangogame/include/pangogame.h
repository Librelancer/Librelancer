// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#ifndef _PANGO_GAME_H_
#define _PANGO_GAME_H_
#ifdef __cplusplus
extern "C" {
#endif

/** TYPES **/
typedef struct _PGQuad {
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

typedef struct _PGTexturePrivate PGTexturePrivate;

typedef struct _PGTexture {
	void* userdata;
} PGTexture;

typedef struct _PGRenderContext PGRenderContext;
typedef struct _PGBuiltText PGBuiltText;

/** CALLBACKS **/
typedef void (*PGDrawCallback)(PGQuad* quads, PGTexture *texture, int count);
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

PGBuiltText *pg_buildtext(PGRenderContext* ctx, const char *markup, int width);
void pg_drawtext(PGRenderContext* ctx, PGBuiltText *text);
void pg_destroytext(PGBuiltText *text);
void pg_destroycontext(PGRenderContext *ctx);

#ifdef __cplusplus
}
#endif
#endif
