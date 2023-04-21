cmake_minimum_required(VERSION 3.1)
project(shadercompiler)

# Disable unused spirv items
set(SPIRV_CROSS_ENABLE_TESTS OFF CACHE BOOL "" FORCE)
set(SPIRV_CROSS_ENABLE_HLSL OFF CACHE BOOL "" FORCE)
set(SPIRV_CROSS_CLI OFF CACHE BOOL "" FORCE)
set(SPIRV_CROSS_ENABLE_MSL OFF CACHE BOOL "" FORCE)
set(SPIRV_CROSS_ENABLE_CPP OFF CACHE BOOL "" FORCE)
set(SPIRV_CROSS_ENABLE_REFLECT OFF CACHE BOOL "" FORCE)
set(SPIRV_CROSS_FORCE_PIC ON CACHE BOOL "" FORCE)
# Disable unused glslang items
set(BUILD_EXTERNAL OFF CACHE BOOL "" FORCE)
set(ENABLE_HLSL OFF CACHE BOOL "" FORCE)
set(ENABLE_SPVREMAPPER OFF CACHE BOOL "" FORCE)
set(ENABLE_GLSLANG_BINARIES OFF CACHE BOOL "" FORCE)

add_subdirectory(../../extern/SPIRV-Cross spirvcross)
add_subdirectory(../../extern/glslang glslang)

add_library(shadercompiler SHARED gl2spv.cpp shadercompiler.cpp)

target_link_libraries(shadercompiler
    glslang 
    glslang-default-resource-limits 
    SPIRV 
    spirv-cross-core 
    spirv-cross-glsl 
    spirv-cross-util 
)
