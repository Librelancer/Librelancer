cmake_minimum_required (VERSION 3.6)
if(UNIX)
set (CMAKE_C_STANDARD 99)
project(pangogame)

find_package(PkgConfig REQUIRED)
pkg_check_modules(PANGO REQUIRED IMPORTED_TARGET pango cairo pangocairo)
add_library(pangogame SHARED
    src/pangogame.cpp
    src/pangorender.cpp
    src/stb.cpp
)
target_include_directories(pangogame PUBLIC "./include")
target_link_libraries(pangogame PkgConfig::PANGO)
endif()
