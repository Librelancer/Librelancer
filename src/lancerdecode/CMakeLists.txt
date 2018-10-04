cmake_minimum_required (VERSION 2.6)
project (lancerdecode)
set(CMAKE_BUILD_TYPE release)
 
add_library(lancerdecode SHARED
	autoload.c
	flac.c
	logging.c
	mp3.c
	riff.c
	stream.c
	vorbis.c
)
