cmake_minimum_required (VERSION 2.6)
project (cimgui)
set(CMAKE_BUILD_TYPE release)
set(CMAKE_CXX_STANDARD 11)
include_directories("./freetypeshim" "../../extern/cimgui/imgui" "../../extern/ImGuiColorTextEdit")
add_library(cimgui SHARED
    ../../extern/cimgui/imgui/imgui.cpp
    ../../extern/cimgui/imgui/imgui_draw.cpp
	../../extern/cimgui/imgui/imgui_widgets.cpp
    ../../extern/cimgui/imgui/imgui_demo.cpp
    ../../extern/cimgui/cimgui.cpp
	../../extern/ImGuiColorTextEdit/TextEditor.cpp
    cimgui_dock.cpp
	cimgui_colortextedit.cpp
    imgui_freetype.cpp
    cimgui_freetype.cpp
	cimgui_spinner.cpp
	cimgui_memory_editor.cpp
    ftshim.c
)
