cmake_minimum_required (VERSION 2.8.12)
project (cimgui)
set(CMAKE_CXX_STANDARD 14)
include_directories("./freetypeshim" "../../extern/cimgui/imgui" "../../extern/ImGuiColorTextEdit" "../../extern/imgui-node-editor")

add_library(cimgui SHARED
    ../../extern/cimgui/imgui/imgui.cpp
    ../../extern/cimgui/imgui/imgui_draw.cpp
	../../extern/cimgui/imgui/imgui_widgets.cpp
    ../../extern/cimgui/imgui/imgui_demo.cpp
    ../../extern/cimgui/imgui/imgui_tables.cpp
    ../../extern/cimgui/cimgui.cpp
	../../extern/ImGuiColorTextEdit/TextEditor.cpp
	../../extern/imgui-node-editor/crude_json.cpp
	../../extern/imgui-node-editor/imgui_canvas.cpp
	../../extern/imgui-node-editor/imgui_node_editor_api.cpp
	../../extern/imgui-node-editor/imgui_node_editor.cpp
    cimgui_dock.cpp
	cimgui_colortextedit.cpp
	cimgui_freetype.cpp
    imgui_freetype.cpp
	cimgui_spinner.cpp
	cimgui_memory_editor.cpp
	cimgui_node_editor.cpp
    ftshim.c
    ImGuizmo.cpp
    cimgui_guizmo.cpp
)
target_compile_definitions(cimgui PUBLIC -DIMGUI_ENABLE_FREETYPE -DCIMGUI_FREETYPE -DIMGUI_ENABLE_STB_TRUETYPE)
