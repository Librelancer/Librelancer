cmake_minimum_required (VERSION 2.8.8)
project(nvttso)
set(CMAKE_BUILD_TYPE release)
set(CMAKE_SKIP_BUILD_RPATH true)
include(FindOpenMP)
if(OPENMP_FOUND)
	SET(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} ${OpenMP_C_FLAGS}")
	SET(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} ${OpenMP_CXX_FLAGS}")
endif()
add_definitions(-DNV_NO_ASSERT -D'nvCheckMacro\(x\)=\(x\)')
add_subdirectory(../../extern/nvidia-texture-tools nvtt)
add_library(nvttso SHARED main.c)
set_target_properties(nvttso PROPERTIES
	OUTPUT_NAME "nvtt"
)
if (${CMAKE_SYSTEM_NAME} MATCHES "Darwin")

target_link_libraries(
	nvttso
	-Wl,-all_load nvcore
	nvimage
	nvmath
	nvthread
	squish
	bc6h
	bc7
	rg_etc1
	nvtt -Wl,-noall_load
)

else()

target_link_libraries(
	nvttso
	-Wl,--whole-archive nvcore
	nvimage
	nvmath
	nvthread
	squish
	bc6h
	bc7
	rg_etc1
	nvtt -Wl,--no-whole-archive
)

endif()


