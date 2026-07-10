cmake_minimum_required (VERSION 3.15)
project (cimgui)

add_library(cimgui SHARED
    ../../extern/imgui/imgui.cpp
    ../../extern/imgui/imgui_draw.cpp
    ../../extern/imgui/imgui_widgets.cpp
    ../../extern/imgui/imgui_demo.cpp
    ../../extern/imgui/imgui_tables.cpp
    ImGuiColorTextEdit/TextEditor.cpp
    ImGuiColorTextEdit/cimgui_colortextedit.cpp
    imgui-node-editor/imgui_canvas.cpp
    imgui-node-editor/imgui_node_editor_api.cpp
    imgui-node-editor/imgui_node_editor.cpp
    imgui-node-editor/cimgui_node_editor.cpp
    implot/implot.cpp
    implot/implot_items.cpp
    implot/implot_demo.cpp
    implot/cimplot.cpp
    implot/cimplot_manual.cpp
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

target_include_directories(cimgui PRIVATE "." "../../extern/imgui" "ImGuiColorTextEdit" "imgui-node-editor" "include")

target_compile_definitions (cimgui PRIVATE -DBUILDING_CIMGUI)
target_compile_definitions(cimgui PUBLIC -DIMGUI_USER_CONFIG=<imconfig_ext.h>)

set_target_properties(cimgui PROPERTIES C_VISIBILITY_PRESET hidden)
set_target_properties(cimgui PROPERTIES CXX_VISIBILITY_PRESET hidden)

if(${CMAKE_SYSTEM_NAME} MATCHES "Windows" AND ${CMAKE_CXX_COMPILER_ID} MATCHES "GNU")
    # link libgcc/libstdc++ into our .dll
    target_link_options(cimgui PRIVATE -static-libgcc -static-libstdc++ -static)
endif()

if(NOT WIN32 AND NOT APPLE)
    find_package(Freetype REQUIRED)
    target_link_libraries(cimgui PRIVATE ${FREETYPE_LIBRARIES})
    target_include_directories(cimgui PRIVATE ${FREETYPE_INCLUDE_DIRS})
else()
    # macOS: no system freetype; use blurgtext's vendored static
    # freetype+harfbuzz (BT_BUILD_FTHB), same as the Windows build
    target_link_libraries(cimgui PRIVATE FREETYPE_LIBRARY HARFBUZZ_LIBRARY)
    if(APPLE)
        # static harfbuzz includes the CoreText shaper
        find_library(COREFOUNDATION_FRAMEWORK CoreFoundation REQUIRED)
        find_library(CORETEXT_FRAMEWORK CoreText REQUIRED)
        find_library(COREGRAPHICS_FRAMEWORK CoreGraphics REQUIRED)
        target_link_libraries(cimgui PRIVATE ${COREFOUNDATION_FRAMEWORK} ${CORETEXT_FRAMEWORK} ${COREGRAPHICS_FRAMEWORK})
    endif()
endif()
