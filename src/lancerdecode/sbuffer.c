#include "sbuffer.h"
#include <string.h>
#include <stdlib.h>

//read 512 bytes at a time
#define READ_BUFFER_SIZE 512

typedef struct {
    ld_stream_t source;
    int32_t filePos;
    int32_t readOffset;
    int32_t readLength;
    int32_t bufferFilled;
    char readbuffer[READ_BUFFER_SIZE];
} sbuffer_userdata_t;

static size_t sbuffer_read(void* ptr, size_t size, size_t count, ld_stream_t stream)
{
    sbuffer_userdata_t *userdata = (sbuffer_userdata_t*)stream->userData;
	int32_t sz_bytes = (int32_t)(size * count);
    if(sz_bytes <= 0) return 0;
    //Read after seek/init
    if(!userdata->bufferFilled) {
        userdata->readOffset = 0;
        userdata->filePos += userdata->readLength;
        userdata->readLength = (int32_t)userdata->source->read((void*)userdata->readbuffer, READ_BUFFER_SIZE, 1, userdata->source);
        userdata->bufferFilled = 1;
    }
    if(!userdata->readLength) return 0;
    int32_t total_bytes = 0;
    while(total_bytes < sz_bytes) {
        int32_t copyAmount = sz_bytes - total_bytes;
        int32_t maxBytes = userdata->readLength - userdata->readOffset;
        if(maxBytes < copyAmount) copyAmount = maxBytes;
        memcpy(((char*)ptr) + total_bytes,(void*)&userdata->readbuffer[userdata->readOffset], copyAmount);
        userdata->readOffset += copyAmount;
        total_bytes += copyAmount;
        if(total_bytes < sz_bytes || userdata->readOffset >= userdata->readLength) {        
            userdata->readOffset = 0;
            userdata->filePos += userdata->readLength;
            userdata->readLength = (int32_t)userdata->source->read((void*)userdata->readbuffer, READ_BUFFER_SIZE, 1, userdata->source);
            userdata->bufferFilled = 1;
            if(!userdata->readLength) return total_bytes;
        }
    }
    return total_bytes;
}

static int sbuffer_tell(ld_stream_t stream)
{    
    sbuffer_userdata_t *userdata = (sbuffer_userdata_t*)stream->userData;
    return userdata->filePos + userdata->readOffset;
}

static int sbuffer_seek(ld_stream_t stream, int32_t offset, LDSEEK origin)
{
    sbuffer_userdata_t *userdata = (sbuffer_userdata_t*)stream->userData;
    if(origin == LDSEEK_CUR) offset += userdata->readOffset;
    userdata->filePos = userdata->source->seek(userdata->source, offset, origin);
    userdata->readOffset = 0;
    userdata->readLength = 0;
    userdata->bufferFilled = 0;
    return userdata->filePos;
}

static void sbuffer_close(ld_stream_t stream)
{
    sbuffer_userdata_t *userdata = (sbuffer_userdata_t*)stream->userData;
    userdata->source->close(userdata->source);
    free(userdata);
    free(stream);
}

ld_stream_t sbuffer_create(ld_stream_t basestream)
{
    sbuffer_userdata_t *userdata = (sbuffer_userdata_t*)malloc(sizeof(sbuffer_userdata_t));
    userdata->source = basestream;
    userdata->filePos = basestream->tell(basestream);
    userdata->readOffset = 0;
    userdata->readLength = 0;
    userdata->bufferFilled = 0;
    ld_stream_t stream = ld_stream_new();
    stream->userData = userdata;
    stream->read = &sbuffer_read;
    stream->seek = &sbuffer_seek;
    stream->tell = &sbuffer_tell;
    stream->close = &sbuffer_close;
    return stream;
} 

void sbuffer_free(ld_stream_t stream)
{
    free(stream->userData);
    free(stream);
}
