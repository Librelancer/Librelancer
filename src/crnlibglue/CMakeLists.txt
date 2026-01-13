cmake_minimum_required (VERSION 3.15)
project (crnlibglue)
set(CMAKE_CXX_STANDARD 11)
set(CRUNCH_DIR "../../extern/crunch")
include_directories(${CRUNCH_DIR}/inc ${CRUNCH_DIR}/crnlib)

if(WIN32)
  set(CRNLIB_THREAD_SRCS
    ${CRUNCH_DIR}/crnlib/lzma_LzFindMt.cpp
    ${CRUNCH_DIR}/crnlib/lzma_Threads.cpp
    ${CRUNCH_DIR}/crnlib/crn_threading_win32.cpp
  )
else(WIN32)
  set(CRNLIB_THREAD_SRCS
    ${CRUNCH_DIR}/crnlib/crn_threading_pthreads.cpp
  )
endif(WIN32)

add_library(
crnlibglue SHARED
${CRUNCH_DIR}/crnlib/crnlib.cpp
${CRUNCH_DIR}/crnlib/crn_arealist.cpp
${CRUNCH_DIR}/crnlib/crn_assert.cpp
${CRUNCH_DIR}/crnlib/crn_checksum.cpp
${CRUNCH_DIR}/crnlib/crn_colorized_console.cpp
${CRUNCH_DIR}/crnlib/crn_command_line_params.cpp
${CRUNCH_DIR}/crnlib/crn_comp.cpp
${CRUNCH_DIR}/crnlib/crn_console.cpp
${CRUNCH_DIR}/crnlib/crn_core.cpp
${CRUNCH_DIR}/crnlib/crn_data_stream.cpp
${CRUNCH_DIR}/crnlib/crn_dds_comp.cpp
${CRUNCH_DIR}/crnlib/crn_decomp.cpp
${CRUNCH_DIR}/crnlib/crn_dxt.cpp
${CRUNCH_DIR}/crnlib/crn_dxt1.cpp
${CRUNCH_DIR}/crnlib/crn_dxt5a.cpp
${CRUNCH_DIR}/crnlib/crn_dxt_endpoint_refiner.cpp
${CRUNCH_DIR}/crnlib/crn_dxt_fast.cpp
${CRUNCH_DIR}/crnlib/crn_dxt_hc.cpp
${CRUNCH_DIR}/crnlib/crn_dxt_hc_common.cpp
${CRUNCH_DIR}/crnlib/crn_dxt_image.cpp
${CRUNCH_DIR}/crnlib/crn_dynamic_string.cpp
${CRUNCH_DIR}/crnlib/crn_etc.cpp
${CRUNCH_DIR}/crnlib/crn_file_utils.cpp
${CRUNCH_DIR}/crnlib/crn_find_files.cpp
${CRUNCH_DIR}/crnlib/crn_hash.cpp
${CRUNCH_DIR}/crnlib/crn_hash_map.cpp
${CRUNCH_DIR}/crnlib/crn_huffman_codes.cpp
${CRUNCH_DIR}/crnlib/crn_image_utils.cpp
${CRUNCH_DIR}/crnlib/crn_jpgd.cpp
${CRUNCH_DIR}/crnlib/crn_jpge.cpp
${CRUNCH_DIR}/crnlib/crn_ktx_texture.cpp
${CRUNCH_DIR}/crnlib/crn_lzma_codec.cpp
${CRUNCH_DIR}/crnlib/crn_math.cpp
${CRUNCH_DIR}/crnlib/crn_mem.cpp
${CRUNCH_DIR}/crnlib/crn_miniz.cpp
${CRUNCH_DIR}/crnlib/crn_mipmapped_texture.cpp
${CRUNCH_DIR}/crnlib/crn_pixel_format.cpp
${CRUNCH_DIR}/crnlib/crn_platform.cpp
${CRUNCH_DIR}/crnlib/crn_prefix_coding.cpp
${CRUNCH_DIR}/crnlib/crn_qdxt1.cpp
${CRUNCH_DIR}/crnlib/crn_qdxt5.cpp
${CRUNCH_DIR}/crnlib/crn_rand.cpp
${CRUNCH_DIR}/crnlib/crn_resampler.cpp
${CRUNCH_DIR}/crnlib/crn_resample_filters.cpp
${CRUNCH_DIR}/crnlib/crn_rg_etc1.cpp
${CRUNCH_DIR}/crnlib/crn_ryg_dxt.cpp
${CRUNCH_DIR}/crnlib/crn_sparse_bit_array.cpp
${CRUNCH_DIR}/crnlib/crn_stb_image.cpp
${CRUNCH_DIR}/crnlib/crn_strutils.cpp
${CRUNCH_DIR}/crnlib/crn_symbol_codec.cpp
${CRUNCH_DIR}/crnlib/crn_texture_comp.cpp
${CRUNCH_DIR}/crnlib/crn_texture_conversion.cpp
${CRUNCH_DIR}/crnlib/crn_texture_file_types.cpp
${CRUNCH_DIR}/crnlib/crn_threaded_resampler.cpp
${CRUNCH_DIR}/crnlib/crn_timer.cpp
${CRUNCH_DIR}/crnlib/crn_utils.cpp
${CRUNCH_DIR}/crnlib/crn_value.cpp
${CRUNCH_DIR}/crnlib/crn_vector.cpp
${CRUNCH_DIR}/crnlib/lzma_7zBuf.cpp
${CRUNCH_DIR}/crnlib/lzma_7zBuf2.cpp
${CRUNCH_DIR}/crnlib/lzma_7zCrc.cpp
${CRUNCH_DIR}/crnlib/lzma_7zFile.cpp
${CRUNCH_DIR}/crnlib/lzma_7zStream.cpp
${CRUNCH_DIR}/crnlib/lzma_Alloc.cpp
${CRUNCH_DIR}/crnlib/lzma_Bcj2.cpp
${CRUNCH_DIR}/crnlib/lzma_Bra.cpp
${CRUNCH_DIR}/crnlib/lzma_Bra86.cpp
${CRUNCH_DIR}/crnlib/lzma_BraIA64.cpp
${CRUNCH_DIR}/crnlib/lzma_LzFind.cpp
${CRUNCH_DIR}/crnlib/lzma_LzmaDec.cpp
${CRUNCH_DIR}/crnlib/lzma_LzmaEnc.cpp
${CRUNCH_DIR}/crnlib/lzma_LzmaLib.cpp
${CRNLIB_THREAD_SRCS}
crnlibglue.cpp
)

set_target_properties(crnlibglue PROPERTIES C_VISIBILITY_PRESET hidden)
set_target_properties(crnlibglue PROPERTIES CXX_VISIBILITY_PRESET hidden)
target_compile_definitions(crnlibglue PRIVATE -DBUILDING_CRNLIB)

if(${CMAKE_SYSTEM_NAME} MATCHES "Windows" AND ${CMAKE_CXX_COMPILER_ID} MATCHES "GNU")
    # link libgcc/libstdc++ into our .dll
    target_link_options(crnlibglue PRIVATE -static-libgcc -static-libstdc++ -static)
endif()

