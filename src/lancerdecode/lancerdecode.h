// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#ifndef _LANCERDECODE_H_
#define _LANCERDECODE_H_

#ifdef __cplusplus
extern "C" {
#endif
#include <stddef.h>
#include <stdint.h>
typedef int32_t LDFORMAT;
#define LDFORMAT_MONO8 1
#define LDFORMAT_MONO16 2
#define LDFORMAT_STEREO8 3
#define LDFORMAT_STEREO16 4

typedef int32_t LDSEEK;

#define LDSEEK_SET 1
#define LDSEEK_CUR 2
#define LDSEEK_END 3

#define LDEOF -256

#ifdef _WIN32
#define LDEXPORT __declspec(dllexport)
#else
#define LDEXPORT
#endif

typedef struct ld_stream *ld_stream_t;
typedef struct ld_pcmstream *ld_pcmstream_t;

struct ld_stream {
	size_t (*read)(void*,size_t,size_t,ld_stream_t);
	int (*seek)(ld_stream_t,int32_t,LDSEEK);
	int32_t (*tell)(ld_stream_t); //Only set for base file streams
	void (*close)(ld_stream_t);
	void* userData;
};

LDEXPORT ld_stream_t ld_stream_new();
LDEXPORT ld_stream_t ld_stream_wrap(ld_stream_t src, int32_t len, int closeparent);
LDEXPORT int ld_stream_getc(ld_stream_t stream);
LDEXPORT void ld_stream_destroy(ld_stream_t stream);

struct ld_pcmstream {
	ld_stream_t stream;
	int32_t dataSize;
	int32_t frequency;
	LDFORMAT format;
	int32_t blockSize;
};

LDEXPORT ld_pcmstream_t ld_pcmstream_open(ld_stream_t stream);
LDEXPORT void ld_pcmstream_close(ld_pcmstream_t stream);

typedef void (*ld_errorlog_callback_t)(const char*);
LDEXPORT void ld_errorlog_register(ld_errorlog_callback_t cb);
#ifdef __cplusplus
}
#endif
#endif
