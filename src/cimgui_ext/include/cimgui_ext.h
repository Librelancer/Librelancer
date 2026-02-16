// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#ifndef _CIMGUI_DOCK_H_
#define _CIMGUI_DOCK_H_

#ifdef __cplusplus
extern "C" {
#endif

#ifndef CIMGUI_API
#ifdef _WIN32
#if BUILDING_CIMGUI
#define CIMGUI_API __declspec(dllexport)
#else
#define CIMGUI_API __declspec(dllimport)
#endif
#else
#define CIMGUI_API __attribute__((visibility("default")))
#endif
#endif

#include <stddef.h>
#include <stdint.h>
//version
CIMGUI_API const char* igExtGetVersion();
//assert
typedef void (*assertion_fail_handler)(const char*, const char*, int);
CIMGUI_API void igInstallAssertHandler(assertion_fail_handler handler);
//custom controls
CIMGUI_API bool igExtSplitterV(float thickness, float* size1, float *size2, float min_size1, float min_size2, float splitter_long_axis_size);
CIMGUI_API bool igExtSpinner(const char* label, float radius, int thickness, int color);
CIMGUI_API bool igExtComboButton(const char* id, const char* preview_value);
CIMGUI_API void igExtRenderArrow(float frameX, float frameY);
CIMGUI_API int igExtPlot(int plotType, const char* label, float (*values_getter)(void* data, int idx), int (*get_tooltip)(void* data, int idx, char* buffer), void* data, int values_count, int values_offset, const char* overlay_text, float scale_min, float scale_max, float size_x, float size_y);
CIMGUI_API bool igButtonEx2(const char* label, float sizeX, float sizeY, int drawFlags);
CIMGUI_API void igExtUseTitlebar(float *restoreX, float *restoreY);
//draw list
CIMGUI_API void igExtDrawListAddTriangleMesh(void* drawlist, float* vertices, int32_t count, uint32_t color);
//font
CIMGUI_API bool igBuildFontAtlas(void* atlas);
//memory editor
typedef void* memoryedit_t;

CIMGUI_API memoryedit_t igExtMemoryEditInit();
CIMGUI_API void igExtMemoryEditDrawContents(memoryedit_t memedit, void *mem_data_void_ptr, size_t mem_size, size_t base_display_addr);
CIMGUI_API void igExtMemoryEditFree(memoryedit_t memedit);

//
CIMGUI_API int igExtInputFloat(const char* label, float* v, float step, float step_fast, const char* format, int flags);
CIMGUI_API int igExtInputFloat2(const char* label, float v[2], const char* format, int flags);
CIMGUI_API int igExtInputFloat3(const char* label, float v[3], const char* format, int flags);
CIMGUI_API int igExtInputFloat4(const char* label, float v[4], const char* format, int flags);
CIMGUI_API int igExtInputInt(const char* label, int* v, int step, int step_fast, int flags);
CIMGUI_API int igExtInputInt2(const char* label, int v[2], int flags);
CIMGUI_API int igExtInputInt3(const char* label, int v[3], int flags);
CIMGUI_API int igExtInputInt4(const char* label, int v[4], int flags);
CIMGUI_API int igExtInputDouble(const char* label, double* v, double step, double step_fast, const char* format, int flags);
CIMGUI_API int igExtInputIntPreview(const char *label, const char *preview, int* v);

//layout hack
CIMGUI_API void igTableFullRowBegin();
CIMGUI_API void igTableFullRowEnd();

//text editor
typedef void *texteditor_t;
typedef enum texteditor_mode {
    TEXTEDITOR_MODE_NORMAL,
    TEXTEDITOR_MODE_LUA
} texteditor_mode_t;
CIMGUI_API texteditor_t igExtTextEditorInit();
CIMGUI_API const char *igExtTextEditorGetText(texteditor_t textedit);
CIMGUI_API void igExtTextEditorSetMode(texteditor_t textedit, texteditor_mode_t mode);
CIMGUI_API void igExtTextEditorSetReadOnly(texteditor_t textedit, int readonly);
CIMGUI_API void igExtFree(void *mem);
CIMGUI_API void igExtTextEditorSetText(texteditor_t textedit, const char *text);
CIMGUI_API int igExtTextEditorGetUndoIndex(texteditor_t textedit);
CIMGUI_API void igExtTextEditorGetCoordinates(texteditor_t textedit, int32_t *x, int32_t *y);
CIMGUI_API void igExtTextEditorRender(texteditor_t textedit, const char *id);
CIMGUI_API void igExtTextEditorFree(texteditor_t textedit);
//guizmo
CIMGUI_API void igGuizmoBeginFrame();
CIMGUI_API void igGuizmoSetOrthographic(int orthographic);
CIMGUI_API int igGuizmoIsUsing();
CIMGUI_API int igGuizmoIsOver();
CIMGUI_API void igGuizmoSetID(int id);
CIMGUI_API void igGuizmoSetRect(float x, float y, float width, float height);
CIMGUI_API int igGuizmoManipulate(float* view, float* projection, int operation, int mode, float* matrix, float* delta);
CIMGUI_API void igGuizmoSetDrawlist();
CIMGUI_API void igGuizmoSetImGuiContext(void* ctx);
//node editor

typedef void* axNodeId;
typedef void* axLinkId;
typedef void* axPinId;

typedef enum
{
    axPinKind_Input,
    axPinKind_Output,
} axPinKind;

typedef enum
{
    axFlowDirection_Forward,
    axFlowDirection_Backward
} axFlowDirection;

typedef enum
{
    axCanvasSizeMode_FitVerticalView,
    axCanvasSizeMode_FitHorizontalView,
    axCanvasSizeMode_CenterOnly
} axCanvasSizeMode;

typedef enum
{
    axSaveReasonFlags_None       = 0x00000000,
    axSaveReasonFlags_Navigation = 0x00000001,
    axSaveReasonFlags_Position   = 0x00000002,
    axSaveReasonFlags_Size       = 0x00000004,
    axSaveReasonFlags_Selection  = 0x00000008,
    axSaveReasonFlags_AddNode    = 0x00000010,
    axSaveReasonFlags_RemoveNode = 0x00000020,
    axSaveReasonFlags_User       = 0x00000040
} axSaveReasonFlags;

typedef enum axStyleColor
{
    axStyleColor_Bg,
    axStyleColor_Grid,
    axStyleColor_NodeBg,
    axStyleColor_NodeBorder,
    axStyleColor_HovNodeBorder,
    axStyleColor_SelNodeBorder,
    axStyleColor_NodeSelRect,
    axStyleColor_NodeSelRectBorder,
    axStyleColor_HovLinkBorder,
    axStyleColor_SelLinkBorder,
    axStyleColor_HighlightLinkBorder,
    axStyleColor_LinkSelRect,
    axStyleColor_LinkSelRectBorder,
    axStyleColor_PinRect,
    axStyleColor_PinRectBorder,
    axStyleColor_Flow,
    axStyleColor_FlowMarker,
    axStyleColor_GroupBg,
    axStyleColor_GroupBorder,

    axStyleColor_Count
} axStyleColor;

typedef enum
{
    axStyleVar_NodePadding,
    axStyleVar_NodeRounding,
    axStyleVar_NodeBorderWidth,
    axStyleVar_HoveredNodeBorderWidth,
    axStyleVar_SelectedNodeBorderWidth,
    axStyleVar_PinRounding,
    axStyleVar_PinBorderWidth,
    axStyleVar_LinkStrength,
    axStyleVar_SourceDirection,
    axStyleVar_TargetDirection,
    axStyleVar_ScrollDuration,
    axStyleVar_FlowMarkerDistance,
    axStyleVar_FlowSpeed,
    axStyleVar_FlowDuration,
    axStyleVar_PivotAlignment,
    axStyleVar_PivotSize,
    axStyleVar_PivotScale,
    axStyleVar_PinCorners,
    axStyleVar_PinRadius,
    axStyleVar_PinArrowSize,
    axStyleVar_PinArrowWidth,
    axStyleVar_GroupRounding,
    axStyleVar_GroupBorderWidth,
    axStyleVar_HighlightConnectedLinks,
    axStyleVar_SnapLinkToPinDir,
    axStyleVar_HoveredNodeBorderOffset,
    axStyleVar_SelectedNodeBorderOffset,

    axStyleVar_Count
} axStyleVar;

typedef struct {
    float x;
    float y;
    float z;
    float w;
} axVec4;

typedef struct {
    float x;
    float y;
} axVec2;

typedef struct {
    axVec4  NodePadding;
    float   NodeRounding;
    float   NodeBorderWidth;
    float   HoveredNodeBorderWidth;
    float   HoverNodeBorderOffset;
    float   SelectedNodeBorderWidth;
    float   SelectedNodeBorderOffset;
    float   PinRounding;
    float   PinBorderWidth;
    float   LinkStrength;
    axVec2  SourceDirection;
    axVec2  TargetDirection;
    float   ScrollDuration;
    float   FlowMarkerDistance;
    float   FlowSpeed;
    float   FlowDuration;
    axVec2  PivotAlignment;
    axVec2  PivotSize;
    axVec2  PivotScale;
    float   PinCorners;
    float   PinRadius;
    float   PinArrowSize;
    float   PinArrowWidth;
    float   GroupRounding;
    float   GroupBorderWidth;
    float   HighlightConnectedLinks;
    float   SnapLinkToPinDir; // when true link will start on the line defined by pin direction
    axVec4  Colors[axStyleColor_Count];
} axStyle;

typedef struct
{
    axVec2 StartPosition;
    axVec2 EndPosition;
    axVec2 StartSize;
    axVec2 EndSize;
    axVec2 StartGroupSize;
    axVec2 EndGroupSize;
} axResizeCallbackData;

CIMGUI_API void axStyle_Init(axStyle *style);

struct axConfig;

typedef int (*axConfigSaveSettings)(const char* data, size_t size, axSaveReasonFlags reason, void* userPointer);
typedef size_t (*axConfigLoadSettings)(char* data, void* userPointer);
typedef int (*axConfigSaveNodeSettings)(axNodeId nodeId, const char* data, size_t size, axSaveReasonFlags reason, void* userPointer);
typedef size_t (*axConfigLoadNodeSettings)(axNodeId nodeId, char* data, void* userPointer);
typedef void (*axConfigSession)(void* userPointer);
typedef void (*axNodeDraggedCallback)(axNodeId nodeId, float oldX, float oldY, float newX, float newY, void* userPointer);
typedef void (*axNodeResizedCallback)(axNodeId nodeId, axResizeCallbackData *data, void* userPointer);

CIMGUI_API axConfig* axConfigNew();
CIMGUI_API void axConfigFree(axConfig* config);

CIMGUI_API const char* axConfig_get_SettingsFile(axConfig* config);
CIMGUI_API void axConfig_set_SettingsFile(axConfig* config, const char *settingsFile);

CIMGUI_API axConfigSession axConfig_get_BeginSaveSession(axConfig *config);
CIMGUI_API void axConfig_set_BeginSaveSession(axConfig *config,  axConfigSession beginSaveSession);

CIMGUI_API axConfigSession axConfig_get_EndSaveSession(axConfig *config);
CIMGUI_API void axConfig_set_EndSaveSession(axConfig *config,  axConfigSession endSaveSession);

CIMGUI_API axConfigSaveSettings axConfig_get_SaveSettings(axConfig *config);
CIMGUI_API void axConfig_set_SaveSettings(axConfig *config, axConfigSaveSettings saveSettings);

CIMGUI_API axConfigLoadSettings axConfig_get_LoadSettings(axConfig *config);
CIMGUI_API void axConfig_set_LoadSettings(axConfig *config, axConfigLoadSettings loadSettings);

CIMGUI_API axConfigSaveNodeSettings axConfig_get_SaveNodeSettings(axConfig *config);
CIMGUI_API void axConfig_set_SaveNodeSettings(axConfig *config, axConfigSaveNodeSettings saveNodeSettings);

CIMGUI_API axConfigLoadNodeSettings axConfig_get_LoadNodeSettings(axConfig *config);
CIMGUI_API void axConfig_set_LoadNodeSettings(axConfig *config, axConfigLoadNodeSettings loadNodeSettings);

CIMGUI_API axNodeDraggedCallback axConfig_get_NodeDraggedHook(axConfig *config);
CIMGUI_API void axConfig_set_NodeDraggedHook(axConfig *config, axNodeDraggedCallback nodeDraggedHook);

CIMGUI_API axNodeDraggedCallback axConfig_get_NodeResizedHook(axConfig *config);
CIMGUI_API void axConfig_set_NodeResizedHook(axConfig *config, axNodeResizedCallback nodeResizedHook);

CIMGUI_API void* axConfig_get_UserPointer(axConfig* config);
CIMGUI_API void axConfig_set_UserPointer(axConfig* config, void* userPointer);

//TODO: Custom Zoom Levels

CIMGUI_API axCanvasSizeMode axConfig_get_CanvasSizeMode(axConfig* config);
CIMGUI_API void axConfig_set_CanvasSizeMode(axConfig* config, axCanvasSizeMode canvasSizeMode);

CIMGUI_API int axConfig_get_DragButtonIndex(axConfig* config);
CIMGUI_API void axConfig_set_DragButtonIndex(axConfig* config, int dragButtonIndex);

CIMGUI_API int axConfig_get_SelectButtonIndex(axConfig* config);
CIMGUI_API void axConfig_set_SelectButtonIndex(axConfig* config, int selectButtonIndex);

CIMGUI_API int axConfig_get_NavigateButtonIndex(axConfig* config);
CIMGUI_API void axConfig_set_NavigateButtonIndex(axConfig* config, int navigateButtonIndex);

CIMGUI_API int axConfig_get_ContextMenuButtonIndex(axConfig* config);
CIMGUI_API void axConfig_set_ContextMenuButtonIndex(axConfig* config, int contextMenuButtonIndex);

CIMGUI_API int axConfig_get_EnableSmoothZoom(axConfig* config);
CIMGUI_API void axConfig_set_EnableSmoothZoom(axConfig* config, int smoothZoom);

CIMGUI_API float axConfig_get_SmoothZoomPower(axConfig* config);
CIMGUI_API void axConfig_set_SmoothZoomPower(axConfig* config, float smoothZoomPower);


struct axEditorContext;

CIMGUI_API void axSetCurrentEditor(axEditorContext* ctx);
CIMGUI_API axEditorContext* axGetCurrentEditor();
CIMGUI_API axEditorContext* axCreateEditor(axConfig* config);
CIMGUI_API void axDestroyEditor(axEditorContext* ctx);
CIMGUI_API const axConfig* axGetConfig(axEditorContext* ctx);

CIMGUI_API axStyle* axGetStyle();
CIMGUI_API const char* axGetStyleColorName(axStyleColor colorIndex);

CIMGUI_API void axPushStyleColor(axStyleColor colorIndex, axVec4* color);
CIMGUI_API void axPopStyleColor(int count);

CIMGUI_API void axPushStyleVar1(axStyleVar varIndex, float value);
CIMGUI_API void axPushStyleVar2(axStyleVar varIndex, axVec2* value);
CIMGUI_API void axPushStyleVar4(axStyleVar varIndex, axVec4* value);
CIMGUI_API void axPopStyleVar(int count);

CIMGUI_API void axBegin(const char* id, axVec2* size);
CIMGUI_API void axEnd();

CIMGUI_API void axBeginNode(axNodeId id);
CIMGUI_API void axBeginPin(axPinId id, axPinKind kind);
CIMGUI_API void axPinRect(axVec2* a, axVec2* b);
CIMGUI_API void axPinPivotRect(axVec2* a, axVec2* b);
CIMGUI_API void axPinPivotSize(axVec2* size);
CIMGUI_API void axPinPivotScale(axVec2* scale);
CIMGUI_API void axPinPivotAlignment(axVec2* alignment);
CIMGUI_API void axEndPin();
CIMGUI_API void axGroup(axVec2* size);
CIMGUI_API void axEndNode();

CIMGUI_API int axBeginGroupHint(axNodeId nodeId);
CIMGUI_API void axGetGroupMin(axVec2 *gmin);
CIMGUI_API void axGetGroupMax(axVec2 *gmax);
CIMGUI_API void* axGetHintForegroundDrawList();
CIMGUI_API void* axGetHintBackgroundDrawList();
CIMGUI_API void axEndGroupHint();

CIMGUI_API void* axGetNodeBackgroundDrawList(axNodeId nodeId);

CIMGUI_API int axLink(axLinkId id, axPinId startPinId, axPinId endPinId, axVec4* color, float thickness);

CIMGUI_API void axFlow(axLinkId linkId, axFlowDirection direction);

CIMGUI_API int axBeginCreate(axVec4* color, float thickness);
CIMGUI_API int axQueryNewLink(axPinId* startId, axPinId* endId);
CIMGUI_API int axQueryNewLink_Styled(axPinId* startId, axPinId* endId, axVec4* color, float thickness);
CIMGUI_API int axQueryNewNode(axPinId* pinId);
CIMGUI_API int axQueryNewNode_Styled(axPinId* pinId, axVec4* color, float thickness);
CIMGUI_API int axAcceptNewItem();
CIMGUI_API int axAcceptNewItem_Styled(axVec4* color, float thickness);
CIMGUI_API void axRejectNewItem();
CIMGUI_API void axRejectNewItem_Styled(axVec4* color, float thickness);
CIMGUI_API void axEndCreate();

CIMGUI_API int axBeginDelete();
CIMGUI_API int axQueryDeletedLink(axLinkId* linkId, axPinId* startId, axPinId* endId);
CIMGUI_API int axQueryDeletedNode(axNodeId* nodeId);
CIMGUI_API int axAcceptDeletedItem(int deleteDependencies);
CIMGUI_API void axRejectDeletedItem();
CIMGUI_API void axEndDelete();

CIMGUI_API void axSetNodePosition(axNodeId nodeId, axVec2* editorPosition);
CIMGUI_API void axSetGroupSize(axNodeId nodeId, axVec2* size);
CIMGUI_API void axGetNodePosition(axNodeId nodeId, axVec2* pos);
CIMGUI_API void axGetNodeSize(axNodeId nodeId, axVec2* sz);
CIMGUI_API void axCenterNodeOnScreen(axNodeId nodeId);
CIMGUI_API void axSetNodeZPosition(axNodeId nodeId, float z); // Sets node z position, nodes with higher value are drawn over nodes with lower value
CIMGUI_API float axGetNodeZPosition(axNodeId nodeId); // Returns node z position, defaults is 0.0f

CIMGUI_API void axRestoreNodeState(axNodeId nodeId);

CIMGUI_API void axSuspend();
CIMGUI_API void axResume();
CIMGUI_API int axIsSuspended();

CIMGUI_API int axIsActive();

CIMGUI_API int axHasSelectionChanged();
CIMGUI_API int axGetSelectedObjectCount();
CIMGUI_API int axGetSelectedNodes(axNodeId* nodes, int size);
CIMGUI_API int axGetSelectedLinks(axLinkId* links, int size);
CIMGUI_API int axIsNodeSelected(axNodeId nodeId);
CIMGUI_API int axIsLinkSelected(axLinkId linkId);
CIMGUI_API void axClearSelection();
CIMGUI_API void axSelectNode(axNodeId nodeId, int append);
CIMGUI_API void axSelectLink(axLinkId linkId, int append);
CIMGUI_API void axDeselectNode(axNodeId nodeId);
CIMGUI_API void axDeselectLink(axLinkId linkId);

CIMGUI_API int axDeleteNode(axNodeId nodeId);
CIMGUI_API int axDeleteLink(axLinkId linkId);

CIMGUI_API int axNodeHasAnyLinks(axNodeId nodeId); // Returns true if node has any link connected
CIMGUI_API int axPinHasAnyLinks(axPinId pinId); // Return true if pin has any link connected
CIMGUI_API int axNodeBreakLinks(axNodeId nodeId); // Break all links connected to this node
CIMGUI_API int axPinBreakLinks(axPinId pinId); // Break all links connected to this pin

CIMGUI_API void axNavigateToContent(float duration);
CIMGUI_API void axNavigateToSelection(int zoomIn, float duration);

CIMGUI_API int axShowNodeContextMenu(axNodeId* nodeId);
CIMGUI_API int axShowPinContextMenu(axPinId* pinId);
CIMGUI_API int axShowLinkContextMenu(axLinkId* linkId);
CIMGUI_API int axShowBackgroundContextMenu();

CIMGUI_API void axEnableShortcuts(int enable);
CIMGUI_API int axAreShortcutsEnabled();

CIMGUI_API int axBeginShortcut();
CIMGUI_API int axAcceptCut();
CIMGUI_API int axAcceptCopy();
CIMGUI_API int axAcceptPaste();
CIMGUI_API int axAcceptDuplicate();
CIMGUI_API int axAcceptCreateNode();
CIMGUI_API int axGetActionContextSize();
CIMGUI_API int axGetActionContextNodes(axNodeId* nodes, int size);
CIMGUI_API int axGetActionContextLinks(axLinkId* links, int size);
CIMGUI_API void axEndShortcut();

CIMGUI_API float axGetCurrentZoom();

CIMGUI_API axNodeId axGetHoveredNode();
CIMGUI_API axPinId axGetHoveredPin();
CIMGUI_API axLinkId axGetHoveredLink();
CIMGUI_API axNodeId axGetDoubleClickedNode();
CIMGUI_API axPinId axGetDoubleClickedPin();
CIMGUI_API axLinkId axGetDoubleClickedLink();
CIMGUI_API int axIsBackgroundClicked();
CIMGUI_API int axIsBackgroundDoubleClicked();
CIMGUI_API int axGetBackgroundClickButtonIndex(); // -1 if none
CIMGUI_API int axGetBackgroundDoubleClickButtonIndex(); // -1 if none

CIMGUI_API int axGetLinkPins(axLinkId linkId, axPinId* startPinId, axPinId* endPinId); // pass nullptr if particular pin do not interest you

CIMGUI_API int axPinHadAnyLinks(axPinId pinId);

CIMGUI_API void axGetScreenSize(axVec2* size);
CIMGUI_API void axScreenToCanvas(axVec2* pos, axVec2* canvas);
CIMGUI_API void axCanvasToScreen(axVec2* pos, axVec2* screen);

CIMGUI_API int axGetNodeCount();
CIMGUI_API int axGetOrderedNodeIds(axNodeId* nodes, int size);

#ifdef __cplusplus
}
#endif
#endif
