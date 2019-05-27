cmake_minimum_required (VERSION 2.8.9)
if(UNIX AND NOT APPLE)
if (CMAKE_VERSION VERSION_LESS "3.1")
    if (CMAKE_C_COMPILER_ID STREQUAL "GNU")
        set (CMAKE_C_FLAGS "-std=gnu99 ${CMAKE_C_FLAGS}")
    endif ()
else ()
    set (CMAKE_C_STANDARD 99)
endif ()
project(pangogame)
include(FindPkgConfig)
pkg_check_modules(PANGO REQUIRED pango pangoft2)
add_library(pangogame SHARED
	src/pangogame.cpp
	src/pangorender.cpp
	src/stb.cpp
)
target_include_directories(pangogame PUBLIC "./include")
target_include_directories(pangogame PRIVATE ${PANGO_INCLUDE_DIRS})
target_link_libraries(pangogame ${PANGO_LIBRARIES})
target_compile_options(pangogame PRIVATE ${PANGO_CFLAGS_OTHER})
endif()
