#include "lancerdecode.h"
#include <stdlib.h>
LDEXPORT ld_stream_t ld_stream_new()
{
	return (ld_stream_t)malloc(sizeof(struct ld_stream));
}

LDEXPORT void ld_stream_destroy(ld_stream_t stream)
{
	free(stream);
}

LDEXPORT int ld_stream_getc(ld_stream_t stream)
{
	uint8_t ch;
	if(stream->read(&ch,1,1,stream)) {
		return ch;
	}
	return LDEOF;
}

typedef struct {
	ld_stream_t source;
	int32_t offset;
	int len;
	int closeparent;
} wrapper_data_t;

#define MIN(x,y) ((x) > (y) ? (y) : (x))

size_t stream_wrapread(void* ptr, size_t size, size_t count, ld_stream_t stream)
{
	wrapper_data_t *data = (wrapper_data_t*)stream->userData;
	int32_t curr = data->source->tell(data->source);
	size = MIN((size_t)(data->len - (curr - data->offset)),size);
	if(size <= 0)
		return 0;
	size_t read = data->source->read(ptr, size, count, data->source);
	return read;
}

int stream_wrapseek(ld_stream_t stream, int32_t offset, LDSEEK origin)
{
	wrapper_data_t *data = (wrapper_data_t*)stream->userData;
	int32_t off = offset;
	if(origin == LDSEEK_SET) {
		off += data->offset;
	}
	if(origin == LDSEEK_END) {
		origin = LDSEEK_SET;
		off = data->offset + data->len + offset;
	}
	return data->source->seek(data->source, off, origin);
} 

int32_t stream_wraptell(ld_stream_t stream)
{
	wrapper_data_t *data= (wrapper_data_t*)stream->userData;
	int32_t src = data->source->tell(data->source);
	return src - data->offset;
}

void stream_wrapclose(ld_stream_t stream)
{
	wrapper_data_t *data = (wrapper_data_t*)stream->userData;
	if(data->closeparent)
		data->source->close(data->source);
	free(data);
	free(stream);
}

LDEXPORT ld_stream_t ld_stream_wrap(ld_stream_t src, int32_t len, int closeparent)
{
	ld_stream_t stream = (ld_stream_t)malloc(sizeof(struct ld_stream));
	wrapper_data_t *data = (wrapper_data_t*)malloc(sizeof(wrapper_data_t));
	data->offset = src->tell(src);
	data->len = len;
	data->source = src;
	data->closeparent = closeparent;
	stream->userData = (void*)data;
	stream->read = &stream_wrapread;
	stream->seek = &stream_wrapseek;
	stream->tell = &stream_wraptell;
	stream->close = &stream_wrapclose;
	return stream;
}

