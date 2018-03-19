cmake_minimum_required (VERSION 2.8.9)
project (librelancernatives)
set(CMAKE_BUILD_TYPE release)
set(NVTT_SHARED 1)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ../binaries)
add_subdirectory(src/cimgui_ext cimgui)
add_subdirectory(extern/BulletSharpPInvoke/libbulletc bulletc)
# nvtt has different project structure
# binary handled by nuget for windows
if(NOT WIN32)
    set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ../../../binaries)
    add_subdirectory(extern/nvidia-texture-tools nvtt)
endif()
