cmake_minimum_required (VERSION 3.15)
project (cimgui)
set(CMAKE_CXX_STANDARD 14)

add_library(cimgui SHARED
    ../../extern/imgui/imgui.cpp
    ../../extern/imgui/imgui_draw.cpp
    ../../extern/imgui/imgui_widgets.cpp
    ../../extern/imgui/imgui_demo.cpp
    ../../extern/imgui/imgui_tables.cpp
    ImGuiColorTextEdit/TextEditor.cpp
    ImGuiColorTextEdit/LanguageDefinitions.cpp
    ImGuiColorTextEdit/ImGuiDebugPanel.cpp
    ImGuiColorTextEdit/UnitTests.cpp
    ImGuiColorTextEdit/cimgui_colortextedit.cpp
    imgui-node-editor/crude_json.cpp
    imgui-node-editor/imgui_canvas.cpp
    imgui-node-editor/imgui_node_editor_api.cpp
    imgui-node-editor/imgui_node_editor.cpp
    imgui-node-editor/cimgui_node_editor.cpp
    controls/cimgui_dock.cpp
    controls/cimgui_expression_input.cpp
    controls/cimgui_guizmo.cpp
    controls/cimgui_memory_editor.cpp
    controls/cimgui_spinner.cpp
    controls/ImGuizmo.cpp
    dcimgui_nodefaultargfunctions.cpp
    dcimgui_manual.cpp
    imgui_freetype.cpp
)

target_compile_features(cimgui PRIVATE cxx_std_20)

target_include_directories(cimgui PRIVATE "." "../../extern/imgui" "ImGuiColorTextEdit" "imgui-node-editor" "include")

target_compile_definitions (cimgui PRIVATE -DBUILDING_CIMGUI)
target_compile_definitions(cimgui PUBLIC -DIMGUI_USER_CONFIG=<imconfig_ext.h>)

target_link_libraries (cimgui PRIVATE boost_regex)

set_target_properties(cimgui PROPERTIES C_VISIBILITY_PRESET hidden)

if(${CMAKE_SYSTEM_NAME} MATCHES "Windows" AND ${CMAKE_CXX_COMPILER_ID} MATCHES "GNU")
    # link libgcc/libstdc++ into our .dll
    target_link_options(cimgui PRIVATE -static-libgcc -static-libstdc++ -static)
endif()

if(NOT WIN32)
    find_package(Freetype REQUIRED)
    target_link_libraries(cimgui PRIVATE ${FREETYPE_LIBRARIES})
    target_include_directories(cimgui PRIVATE ${FREETYPE_INCLUDE_DIRS})
else()
    target_link_libraries(cimgui PRIVATE FREETYPE_LIBRARY HARFBUZZ_LIBRARY)
endif()
