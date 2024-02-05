// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#ifndef _PANGO_GAME_H_
#define _PANGO_GAME_H_
#ifdef __cplusplus
extern "C" {
#endif

#include <stdint.h>
typedef struct _PGTexturePrivate PGTexturePrivate;

typedef struct _PGTexture {
	void* userdata;
} PGTexture;

/** TYPES **/

typedef enum {
	PGAlign_Left = 0,
	PGAlign_Right = 1,
	PGAlign_Center = 2
} PGAlign;

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

typedef struct _PGAttribute {
  int startIndex;
  int endIndex;
  int bold;
  int italic;
  int underline;
  uint32_t fgColor;
  int fontSize;
  const char *fontName;
  int shadowEnabled;
  uint32_t shadowColor;
  int backgroundEnabled;
  uint32_t backgroundColor;
} PGAttribute;
  
typedef struct _PGParagraph {
  const char *text;
  PGAlign alignment;
  PGAttribute *attributes;
  int attributeCount;
} PGParagraph;


typedef struct _PGRenderContext PGRenderContext;
typedef struct _PGBuiltText PGBuiltText;

/** CALLBACKS **/
typedef void (*PGDrawCallback)(PGQuad* quads, int count);
/** Implementation must set userdata variable in PGTexture **/
typedef void (*PGAllocateTextureCallback)(PGTexture *texture, int width, int height, int isColor);
typedef void (*PGUpdateTextureCallback)(PGTexture *texture, void *buffer, int x, int y, int width, int height);

/** FUNCTIONS **/
void pg_addttfglobal(const char *filename);

PGRenderContext *pg_createcontext(
	PGAllocateTextureCallback allocate, 
	PGUpdateTextureCallback update, 
	PGDrawCallback draw
);

  
PGBuiltText *pg_buildtext(PGRenderContext *ctx,
                          PGParagraph     *paragraphs,
                          int              paragraphCount,
                          int              width);
PGBuiltText *pg_buildtext_markup(PGRenderContext* ctx, char **markups, PGAlign* aligns, int paragraphCount, int width);
void pg_drawstring(PGRenderContext* ctx, const char *str, const char* fontName, float fontSize, PGAlign align, float maxWidth, int underline, float r, float g, float b, float a, float *shadow, float *oWidth, float *oHeight);
void pg_measurestring(PGRenderContext* ctx, const char* str, const char* fontName, float fontSize, float maxWidth, float *width, float *height);
float pg_lineheight(PGRenderContext* ctx, const char* fontName, float fontSize);
void pg_drawtext(PGRenderContext* ctx, PGBuiltText *text);
void pg_updatewidth(PGBuiltText *text, int width);
int pg_getheight(PGBuiltText *text);
void pg_get_caret_position(PGBuiltText *text, int paragraph, int textPosition, int *outX, int *outY, int *outW, int *outH);
void pg_destroytext(PGBuiltText *text);
void pg_destroycontext(PGRenderContext *ctx);

#ifdef __cplusplus
}
#endif
#endif
