cmake_minimum_required (VERSION 3.15)
project (cimgui)
set(CMAKE_CXX_STANDARD 14)

add_library(cimgui SHARED
    ../../extern/cimgui/imgui/imgui.cpp
    ../../extern/cimgui/imgui/imgui_draw.cpp
    ../../extern/cimgui/imgui/imgui_widgets.cpp
    ../../extern/cimgui/imgui/imgui_demo.cpp
    ../../extern/cimgui/imgui/imgui_tables.cpp
    ../../extern/cimgui/cimgui.cpp
    ../../extern/ImGuiColorTextEdit/TextEditor.cpp
    ../../extern/ImGuiColorTextEdit/LanguageDefinitions.cpp
    ../../extern/ImGuiColorTextEdit/ImGuiDebugPanel.cpp
    ../../extern/ImGuiColorTextEdit/UnitTests.cpp
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
    ImGuizmo.cpp
    cimgui_guizmo.cpp
)

target_include_directories(cimgui PRIVATE "." "../../extern/cimgui/imgui" "../../extern/ImGuiColorTextEdit" "../../extern/imgui-node-editor" "../../extern/imgui_test_engine/imgui_test_engine")

target_compile_definitions(cimgui PUBLIC -DIMGUI_ENABLE_FREETYPE -DCIMGUI_FREETYPE -DIMGUI_ENABLE_STB_TRUETYPE -DIMGUI_USER_CONFIG=<imconfig_ext.h>)

target_link_libraries (cimgui PRIVATE boost_regex)

if(NOT WIN32)
    find_package(Freetype REQUIRED)
    target_link_libraries(cimgui PRIVATE ${FREETYPE_LIBRARIES})
    target_include_directories(cimgui PRIVATE ${FREETYPE_INCLUDE_DIRS})
else()
    target_link_libraries(cimgui PRIVATE FREETYPE_LIBRARY HARFBUZZ_LIBRARY)
endif()
