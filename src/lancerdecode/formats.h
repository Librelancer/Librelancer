#ifndef _FORMATS_H_
#define _FORMATS_H_
#include "lancerdecode.h"


ld_pcmstream_t riff_getstream(ld_stream_t stream);
ld_pcmstream_t mp3_getstream(ld_stream_t stream);
ld_pcmstream_t ogg_getstream(ld_stream_t stream);
ld_pcmstream_t flac_getstream(ld_stream_t stream, int oggC);

#endif 
