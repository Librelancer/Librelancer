// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#include "lancerdecode.h"
#include "formats.h"
#include "logging.h"
#include <stdlib.h>

#define DR_MP3_IMPLEMENTATION
#define DR_MP3_NO_STDIO
#define MP3_BUFFER_SIZE 8192
#include "dr_mp3.h"


typedef struct {
	drmp3 dec;
	ld_stream_t baseStream;
	void *floatBuffer;
	int floatBufferSize;
	int currentSamples;
	int totalSamples;
	int trimSamples;
} mp3_userdata_t;


size_t read_stream_drmp3(void *pUserData, void *pBufferOut, size_t size)
{
	ld_stream_t stream = (ld_stream_t)pUserData;
	return stream->read(pBufferOut, 1, size, stream);
}

drmp3_bool32 seek_stream_drmp3(void *pUserData, int offset, drmp3_seek_origin origin)
{
	ld_stream_t stream = (ld_stream_t)pUserData;
	int or;
	if(origin == drmp3_seek_origin_start) or = LDSEEK_SET;
	if(origin == drmp3_seek_origin_current) or = LDSEEK_CUR;
	if(stream->seek(stream, offset, or) < 0)
		return DRMP3_FALSE;
	else
		return DRMP3_TRUE;
}
size_t mp3_read(void* ptr, size_t size, size_t count, ld_stream_t stream)
{

	mp3_userdata_t *userdata = (mp3_userdata_t*)stream->userData;
	int sz_bytes = (int)(size * count);
	if((sz_bytes % 2) != 0) {
		LOG_ERROR("mp3_read: buffer size must be a multiple of sizeof(short)");
		return 0;
	}

	int nsamples = sz_bytes / 2;
	if(userdata->totalSamples != -1 && ((nsamples + userdata->currentSamples) > userdata->totalSamples)) {
		nsamples = userdata->totalSamples - userdata->currentSamples;
		if(nsamples <= 0) {
			return (size_t)0;
		}
	}
	int floatsz = nsamples * sizeof(float);
	if(userdata->floatBufferSize != floatsz) {
		if(userdata->floatBuffer) free(userdata->floatBuffer);
		userdata->floatBuffer = malloc(floatsz);
		userdata->floatBufferSize = floatsz;
	}
	int frameCount = nsamples / userdata->dec.channels;
	drmp3_uint64 fcount = drmp3_read_f32(&userdata->dec, (drmp3_uint64)frameCount, (float*)userdata->floatBuffer);
	drmp3dec_f32_to_s16((float*)userdata->floatBuffer, (drmp3_int16*)ptr, (int)(fcount * userdata->dec.channels));
	userdata->currentSamples += (int)(fcount * userdata->dec.channels);
	return (size_t)(fcount * userdata->dec.channels * 2);
}
int mp3_seek(ld_stream_t stream, int32_t offset, LDSEEK origin)
{
	if(origin != LDSEEK_SET || offset != 0) {
		LOG_ERROR("mp3: can only seek to SET 0");
		return -1; 
	}
	mp3_userdata_t *userdata = (mp3_userdata_t*)stream->userData;
	drmp3_seek_to_frame(&userdata->dec, (drmp3_uint64)userdata->trimSamples);
	userdata->currentSamples = userdata->trimSamples;
	return 0;	
}

void mp3_close(ld_stream_t stream)
{
	mp3_userdata_t *userdata = (mp3_userdata_t*)stream->userData;
	userdata->baseStream->close(userdata->baseStream);
	if(userdata->floatBuffer) free(userdata->floatBuffer);
	free(userdata);
	free(stream);
}

ld_pcmstream_t mp3_getstream(ld_stream_t stream, int decodeChannels, int decodeRate, int trimSamples, int totalSamples)
{
	mp3_userdata_t *userdata = (mp3_userdata_t*)malloc(sizeof(mp3_userdata_t));
    drmp3_config drconfig;
	if(decodeRate == -1)
		drconfig.outputSampleRate = 0;
	else
		drconfig.outputSampleRate = decodeRate;
	if(decodeChannels == -1)
		drconfig.outputChannels = 0;
	else
		drconfig.outputChannels = decodeChannels;
	if(!drmp3_init(&userdata->dec,read_stream_drmp3,seek_stream_drmp3,(void*)stream,&drconfig)) {
		LOG_ERROR("drmp3_init failed!");
		free(userdata);
		return NULL;
	}
	userdata->baseStream = stream;
	userdata->floatBuffer = NULL;
	userdata->floatBufferSize = -1;
	userdata->trimSamples = (trimSamples == -1 ? 0 : trimSamples);
	userdata->totalSamples = totalSamples;
	ld_stream_t decodeStream = ld_stream_new();
	decodeStream->userData = (void*)userdata;
	decodeStream->read = mp3_read;
	decodeStream->seek = mp3_seek;
	decodeStream->close = mp3_close;
	if(trimSamples != -1) {
		drmp3_seek_to_frame(&userdata->dec, (drmp3_uint64)trimSamples);
		userdata->currentSamples = trimSamples;
	}
	ld_pcmstream_t retsound = (ld_pcmstream_t)malloc(sizeof(struct ld_pcmstream));
	retsound->dataSize = -1;
	if(userdata->dec.channels == 2) {
		retsound->format = LDFORMAT_STEREO16;
	} else {
		retsound->format = LDFORMAT_MONO16;
	}
	retsound->frequency = (int32_t)userdata->dec.sampleRate;
	retsound->stream = decodeStream;
	retsound->blockSize = MP3_BUFFER_SIZE;
	return retsound;
}
