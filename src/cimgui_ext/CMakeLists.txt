cmake_minimum_required (VERSION 2.6)
project (cimgui)
set(CMAKE_BUILD_TYPE release)

include_directories("./freetypeshim" "../../extern/cimgui/imgui")
add_library(cimgui SHARED
    ../../extern/cimgui/imgui/imgui.cpp
    ../../extern/cimgui/imgui/imgui_draw.cpp
    ../../extern/cimgui/imgui/imgui_demo.cpp
    ../../extern/cimgui/cimgui/cimgui.cpp
    ../../extern/cimgui/cimgui/drawList.cpp
    ../../extern/cimgui/cimgui/fontAtlas.cpp
    ../../extern/cimgui/cimgui/listClipper.cpp
    cimgui_dock.cpp
    imgui_freetype.cpp
    cimgui_freetype.cpp
	cimgui_spinner.cpp
    ftshim.c
)
