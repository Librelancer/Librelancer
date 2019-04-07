cmake_minimum_required (VERSION 2.8.8)
project(nvttso)
set(CMAKE_BUILD_TYPE release)
set(CMAKE_SKIP_BUILD_RPATH true)
add_definitions(-DNV_NO_ASSERT -D'nvCheckMacro\(x\)=\(x\)')
add_subdirectory(../../extern/nvidia-texture-tools nvtt)
add_library(nvttso SHARED main.c)
set_target_properties(nvttso PROPERTIES
	OUTPUT_NAME "nvtt"
)
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

