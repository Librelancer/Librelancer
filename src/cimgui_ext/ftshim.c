// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#include "ftshim.h"
#include <ft2build.h>
#include FT_FREETYPE_H
#include FT_GLYPH_H
#include FT_SYNTHESIS_H

typedef FT_Error (*initFreetypePtr)(FT_Library*);
static initFreetypePtr initFreetype;

FT_EXPORT( FT_Error )
FT_Init_FreeType( FT_Library  *alibrary )
{
	return initFreetype(alibrary);
}

typedef FT_Error (*newMemoryFacePtr)(FT_Library,const FT_Byte*,FT_Long,FT_Long,FT_Face*);
static newMemoryFacePtr newMemoryFace;

FT_EXPORT( FT_Error )
FT_New_Memory_Face( FT_Library      library,
                      const FT_Byte*  file_base,
                      FT_Long         file_size,
                      FT_Long         face_index,
                      FT_Face        *aface )
{
	return newMemoryFace(library,file_base,file_size,face_index,aface);
}

typedef FT_Error (*selectCharmapPtr)(FT_Face,FT_Encoding);
static selectCharmapPtr selectCharmap;

FT_EXPORT( FT_Error )
FT_Select_Charmap( FT_Face      face,
                     FT_Encoding  encoding )
{
	return selectCharmap(face,encoding);
}

typedef FT_Error (*doneFacePtr)(FT_Face);
static doneFacePtr doneFace;

FT_EXPORT( FT_Error )
FT_Done_Face( FT_Face  face )
{
	return doneFace(face);
}

typedef void (*doneGlyphPtr)(FT_Glyph);
static doneGlyphPtr doneGlyph;

FT_EXPORT( void )
FT_Done_Glyph( FT_Glyph glyph )
{
	doneGlyph(glyph);
}

typedef FT_Error(*doneFreetypePtr)(FT_Library);
static doneFreetypePtr doneFreetype;

FT_EXPORT( FT_Error )
FT_Done_FreeType( FT_Library  library )
{
	return doneFreetype(library);
}

typedef FT_Error(*requestSizePtr)(FT_Face,FT_Size_Request);
static requestSizePtr requestSize;

FT_EXPORT( FT_Error )
FT_Request_Size( FT_Face          face,
                   FT_Size_Request  req )
{
	return requestSize(face,req);
}

typedef FT_UInt(*getCharIndexPtr)(FT_Face,FT_ULong);
static getCharIndexPtr getCharIndex;

FT_EXPORT( FT_UInt )
FT_Get_Char_Index( FT_Face   face,
                     FT_ULong  charcode )
{
	return getCharIndex(face,charcode);
}

typedef FT_Error(*loadGlyphPtr)(FT_Face,FT_UInt,FT_Int32);
static loadGlyphPtr loadGlyph;

FT_EXPORT( FT_Error )
FT_Load_Glyph( FT_Face   face,
                 FT_UInt   glyph_index,
                 FT_Int32  load_flags )
{
	return loadGlyph(face,glyph_index,load_flags);
}

typedef void(*emboldenPtr)(FT_GlyphSlot);
static emboldenPtr embolden;

FT_EXPORT( void )
FT_GlyphSlot_Embolden( FT_GlyphSlot  slot )
{
	embolden(slot);
}

typedef void(*obliquePtr)(FT_GlyphSlot);
static obliquePtr oblique;

FT_EXPORT( void )
FT_GlyphSlot_Oblique( FT_GlyphSlot  slot )
{
	oblique(slot);
}

typedef FT_Error(*getGlyphPtr)(FT_GlyphSlot,FT_Glyph*);
static getGlyphPtr getGlyph;

FT_EXPORT( FT_Error )
FT_Get_Glyph( FT_GlyphSlot  slot,
                FT_Glyph     *aglyph )
{
	return getGlyph(slot, aglyph);
}

typedef FT_Error(*glyphToBitmapPtr)(FT_Glyph*,FT_Render_Mode,FT_Vector*,FT_Bool);
static glyphToBitmapPtr glyphToBitmap;

FT_EXPORT( FT_Error )
FT_Glyph_To_Bitmap( FT_Glyph*       the_glyph,
                      FT_Render_Mode  render_mode,
                      FT_Vector*      origin,
                      FT_Bool         destroy )
{
	return glyphToBitmap(the_glyph, render_mode, origin, destroy);
}

typedef FT_Error(*newLibPtr)(FT_Memory,FT_Library*);
static newLibPtr newLib;
FT_EXPORT( FT_Error )
FT_New_Library(FT_Memory memory, FT_Library* alibrary)
{
    return newLib(memory,alibrary);
}

typedef FT_Error(*doneLibPtr)(FT_Library library);
static doneLibPtr doneLib;

FT_EXPORT ( FT_Error )
FT_Done_Library( FT_Library library )
{
    return doneLib(library);
}

typedef void(*newModPtr)(FT_Library library);
static newModPtr newMod;

FT_EXPORT(void)
FT_Add_Default_Modules(FT_Library library)
{
    newMod(library);
}

typedef FT_Error (*renderGlyphPtr)(FT_GlyphSlot slot, FT_Render_Mode render_mode);
static renderGlyphPtr renderGlyph;

FT_EXPORT ( FT_Error )
FT_Render_Glyph ( FT_GlyphSlot slot,
                  FT_Render_Mode render_mode )
{
    return renderGlyph(slot, render_mode);
}

#if (WIN32 || _WIN64)
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#define MODULE HMODULE
#define OPEN_LIB LoadLibraryA("freetype6.dll")
#define dlsym GetProcAddress
#else
#include <dlfcn.h>
#define MODULE void*
#define OPEN_LIB dlopen("libfreetype.so.6", RTLD_NOW);
#endif
void igLoadFreetype(void)
{
	MODULE module;
	module = OPEN_LIB;
	initFreetype = (initFreetypePtr)dlsym(module, "FT_Init_FreeType");
	newMemoryFace = (newMemoryFacePtr)dlsym(module, "FT_New_Memory_Face");
	selectCharmap = (selectCharmapPtr)dlsym(module, "FT_Select_Charmap");
	doneFace = (doneFacePtr)dlsym(module, "FT_Done_Face");
	doneFreetype = (doneFreetypePtr)dlsym(module, "FT_Done_FreeType");
	requestSize = (requestSizePtr)dlsym(module, "FT_Request_Size");
	getCharIndex = (getCharIndexPtr)dlsym(module, "FT_Get_Char_Index");
	loadGlyph = (loadGlyphPtr)dlsym(module,"FT_Load_Glyph");
	embolden = (emboldenPtr)dlsym(module,"FT_GlyphSlot_Embolden");
	oblique = (obliquePtr)dlsym(module, "FT_GlyphSlot_Oblique");
	getGlyph = (getGlyphPtr)dlsym(module, "FT_Get_Glyph");
	glyphToBitmap = (glyphToBitmapPtr)dlsym(module, "FT_Glyph_To_Bitmap");
	doneGlyph = (doneGlyphPtr)dlsym(module, "FT_Done_Glyph");
    newLib = (newLibPtr)dlsym(module, "FT_New_Library");
    doneLib = (doneLibPtr)dlsym(module, "FT_Done_Library");
    newMod = (newModPtr)dlsym(module, "FT_Add_Default_Modules");
    renderGlyph = (renderGlyphPtr)dlsym(module, "FT_Render_Glyph");
    
}
