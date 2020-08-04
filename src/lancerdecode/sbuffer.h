//SBUFFER
//buffers read calls from decoder to parent stream object
//used for e.g. stb_vorbis which calls fgetc a lot and produces many 1 byte reads
#ifndef _SBUFFER_H_
#define _SBUFFER_H_
#include "lancerdecode.h"

ld_stream_t sbuffer_create(ld_stream_t basestream);
//Use when there are errors but you don't want to close the base stream
void sbuffer_free(ld_stream_t sbuffer);

#endif
