// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#include "lancerdecode.h"
#include "formats.h"
#include "logging.h"
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

typedef struct {
	char chunkID[4];
	uint32_t chunkSize;
	char format[4];
} riff_header_t;

typedef struct {
	char subChunkID[4];
	uint32_t subChunkSize;
	uint16_t audioFormat;
	uint16_t numChannels;
	uint32_t sampleRate;
	uint32_t byteRate;
	uint16_t blockAlign;
	uint16_t bitsPerSample;
} wave_format_t;

typedef struct {
  char subChunkID[4];
  uint32_t subChunk2Size;
} wave_data_t;

#define WAVE_FORMAT_PCM 1
#define WAVE_FORMAT_MP3 0x55
#define WAVE_FORMAT_EXTENSIBLE 0xFFFE
#define WAVE_FORMAT_IEEE_FLOAT		0x0003 /* IEEE Float */
#define WAVE_FORMAT_ALAW		0x0006 /* ALAW */
#define WAVE_FORMAT_MULAW		0x0007 /* MULAW */
#define WAVE_FORMAT_IMA_ADPCM		0x0011 /* IMA ADPCM */

ld_pcmstream_t riff_getstream(ld_stream_t stream)
{
	wave_format_t wave_format;
	riff_header_t riff_header;
	wave_data_t wave_data;
	ld_pcmstream_t retsound;

	stream->read(&riff_header, sizeof(riff_header_t), 1, stream);

	if(memcmp(riff_header.chunkID, "RIFF", 4) != 0 ||
		memcmp(riff_header.format, "WAVE", 4) != 0) {
		LOG_ERROR("Invalid RIFF or WAVE header");
		stream->close(stream);
		return 0;
	} 

	stream->read (&wave_format, sizeof(wave_format_t), 1, stream);

	if(memcmp(wave_format.subChunkID, "fmt ", 4) != 0) {
		char actual[5] = { wave_format.subChunkID[0],
		wave_format.subChunkID[1], wave_format.subChunkID[2], wave_format.subChunkID[3], '\0' };
		LOG_ERROR_F("Invalid Wave Format :'%s'", actual);
		stream->close(stream);
		return 0;
	}

	if(wave_format.subChunkSize > 16)
		stream->seek(stream, wave_format.subChunkSize - 16, LDSEEK_CUR);

	int has_data = 0;
	int32_t total_samples = -1;
	int32_t trim_samples = -1;
	while(!has_data) {
		if(!stream->read(&wave_data, sizeof(wave_data_t), 1, stream))
		{
			stream->close(stream);
			LOG_ERROR("Unable to find WAVE data");
			return 0;
		}
		if(memcmp(wave_data.subChunkID, "data", 4) == 0) {
			has_data = 1;
		} else if (memcmp(wave_data.subChunkID, "fact", 4) == 0) {
			//MP3: Total PCM Samples encoded, trims padding
			stream->read(&total_samples, sizeof(int32_t), 1, stream);
			if(wave_data.subChunk2Size - sizeof(int32_t) > 0)
				stream->seek(stream, wave_data.subChunk2Size - sizeof(int32_t), LDSEEK_CUR);
		} else if (memcmp(wave_data.subChunkID, "trim", 4) == 0) {
			//Freelancer MP3: trim samples at start
			stream->read(&trim_samples, sizeof(int32_t), 1, stream); 
			if(wave_data.subChunk2Size - sizeof(int32_t) > 0)
				stream->seek(stream,wave_data.subChunk2Size - sizeof(int32_t), LDSEEK_CUR);
		} else {
			//skip chunk
			stream->seek(stream, wave_data.subChunk2Size, LDSEEK_CUR);
		}
	}
	if(trim_samples == -1)
		total_samples = trim_samples = -1; //Incomplete data, don't bother trimming
	if(total_samples != -1 && trim_samples != -1)
		total_samples = total_samples + trim_samples;
	switch (wave_format.audioFormat) {
		case WAVE_FORMAT_PCM:
			break; //Default decoder
		case WAVE_FORMAT_MP3:
			return mp3_getstream(ld_stream_wrap(stream, wave_data.subChunk2Size, 1),wave_format.numChannels, wave_format.sampleRate, trim_samples, total_samples);
		default:
			LOG_ERROR_F("Unsupported format in WAVE file: '%x'", wave_format.audioFormat);
			stream->close(stream);
			return 0;
	}

	retsound = (ld_pcmstream_t)malloc(sizeof(struct ld_pcmstream));

	if(wave_format.numChannels == 1) {
		if (wave_format.bitsPerSample == 8) {
			retsound->format = LDFORMAT_MONO8;
		} else if (wave_format.bitsPerSample == 16) {
			retsound->format = LDFORMAT_MONO16;
		}
	} else if (wave_format.numChannels == 2) {
		if (wave_format.bitsPerSample == 8) {
			retsound->format = LDFORMAT_STEREO8;
		} else if (wave_format.bitsPerSample == 16) {
			retsound->format = LDFORMAT_STEREO16;
		}
	}

	

	retsound->frequency = wave_format.sampleRate;
	retsound->stream = ld_stream_wrap(stream, wave_data.subChunk2Size, 1);
	retsound->dataSize = wave_data.subChunk2Size;
	retsound->blockSize = 32768;
	return retsound;
}
