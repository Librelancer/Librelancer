// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#ifndef _CIMGUI_DOCK_H_
#define _CIMGUI_DOCK_H_

#ifdef __cplusplus
extern "C" {
#endif
#ifdef _WIN32
#define IGEXPORT __declspec(dllexport)
#else
#define IGEXPORT
#endif
#include <stddef.h>
#include <stdint.h>
//version
IGEXPORT const char* igExtGetVersion();
//custom controls
IGEXPORT bool igExtSplitterV(float thickness, float* size1, float *size2, float min_size1, float min_size2, float splitter_long_axis_size);
IGEXPORT bool igExtSpinner(const char* label, float radius, int thickness, int color);
IGEXPORT bool igExtComboButton(const char* id, const char* preview_value);
IGEXPORT int igExtPlot(int plotType, const char* label, float (*values_getter)(void* data, int idx), int (*get_tooltip)(void* data, int idx, char* buffer), void* data, int values_count, int values_offset, const char* overlay_text, float scale_min, float scale_max, float size_x, float size_y);
IGEXPORT bool igButtonEx2(const char* label, float sizeX, float sizeY, int drawFlags);
//font
IGEXPORT bool igBuildFontAtlas(void* atlas);
//memory editor
typedef void* memoryedit_t;

IGEXPORT memoryedit_t igExtMemoryEditInit();
IGEXPORT void igExtMemoryEditDrawContents(memoryedit_t memedit, void *mem_data_void_ptr, size_t mem_size, size_t base_display_addr);
IGEXPORT void igExtMemoryEditFree(memoryedit_t memedit);

//text editor
typedef void *texteditor_t;
typedef enum texteditor_mode {
    TEXTEDITOR_MODE_NORMAL,
    TEXTEDITOR_MODE_LUA
} texteditor_mode_t;
IGEXPORT texteditor_t igExtTextEditorInit();
IGEXPORT const char *igExtTextEditorGetText(texteditor_t textedit);
IGEXPORT void igExtTextEditorSetMode(texteditor_t textedit, texteditor_mode_t mode);
IGEXPORT void igExtTextEditorSetReadOnly(texteditor_t textedit, int readonly);
IGEXPORT void igExtFree(void *mem);
IGEXPORT void igExtTextEditorSetText(texteditor_t textedit, const char *text);
IGEXPORT int igExtTextEditorIsTextChanged(texteditor_t textedit);
IGEXPORT void igExtTextEditorGetCoordinates(texteditor_t textedit, int32_t *x, int32_t *y);
IGEXPORT void igExtTextEditorRender(texteditor_t textedit, const char *id);
IGEXPORT void igExtTextEditorFree(texteditor_t textedit);
//guizmo
IGEXPORT void igGuizmoBeginFrame();
IGEXPORT void igGuizmoSetOrthographic(int orthographic);
IGEXPORT int igGuizmoIsUsing();
IGEXPORT int igGuizmoIsOver();
IGEXPORT void igGuizmoSetID(int id);
IGEXPORT void igGuizmoSetRect(float x, float y, float width, float height);
IGEXPORT int igGuizmoManipulate(float* view, float* projection, int operation, int mode, float* matrix, float* delta);
IGEXPORT void igGuizmoSetDrawlist();
IGEXPORT void igGuizmoSetImGuiContext(void* ctx);
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

IGEXPORT void axStyle_Init(axStyle *style);

struct axConfig;

typedef int (*axConfigSaveSettings)(const char* data, size_t size, axSaveReasonFlags reason, void* userPointer);
typedef size_t (*axConfigLoadSettings)(char* data, void* userPointer);
typedef int (*axConfigSaveNodeSettings)(axNodeId nodeId, const char* data, size_t size, axSaveReasonFlags reason, void* userPointer);
typedef size_t (*axConfigLoadNodeSettings)(axNodeId nodeId, char* data, void* userPointer);
typedef void (*axConfigSession)(void* userPointer);

IGEXPORT axConfig* axConfigNew();
IGEXPORT void axConfigFree(axConfig* config);

IGEXPORT const char* axConfig_get_SettingsFile(axConfig* config);
IGEXPORT void axConfig_set_SettingsFile(axConfig* config, const char *settingsFile);

IGEXPORT axConfigSession axConfig_get_BeginSaveSession(axConfig *config);
IGEXPORT void axConfig_set_BeginSaveSession(axConfig *config,  axConfigSession beginSaveSession);

IGEXPORT axConfigSession axConfig_get_EndSaveSession(axConfig *config);
IGEXPORT void axConfig_set_EndSaveSession(axConfig *config,  axConfigSession endSaveSession);

IGEXPORT axConfigSaveSettings axConfig_get_SaveSettings(axConfig *config);
IGEXPORT void axConfig_set_SaveSettings(axConfig *config, axConfigSaveSettings saveSettings);

IGEXPORT axConfigLoadSettings axConfig_get_LoadSettings(axConfig *config);
IGEXPORT void axConfig_set_LoadSettings(axConfig *config, axConfigLoadSettings loadSettings);

IGEXPORT axConfigSaveNodeSettings axConfig_get_SaveNodeSettings(axConfig *config);
IGEXPORT void axConfig_set_SaveNodeSettings(axConfig *config, axConfigSaveNodeSettings saveNodeSettings);

IGEXPORT axConfigLoadNodeSettings axConfig_get_LoadNodeSettings(axConfig *config);
IGEXPORT void axConfig_set_LoadNodeSettings(axConfig *config, axConfigLoadNodeSettings loadNodeSettings);


IGEXPORT void* axConfig_get_UserPointer(axConfig* config);
IGEXPORT void axConfig_set_UserPointer(axConfig* config, void* userPointer);

//TODO: Custom Zoom Levels

IGEXPORT axCanvasSizeMode axConfig_get_CanvasSizeMode(axConfig* config);
IGEXPORT void axConfig_set_CanvasSizeMode(axConfig* config, axCanvasSizeMode canvasSizeMode);

IGEXPORT int axConfig_get_DragButtonIndex(axConfig* config);
IGEXPORT void axConfig_set_DragButtonIndex(axConfig* config, int dragButtonIndex);

IGEXPORT int axConfig_get_SelectButtonIndex(axConfig* config);
IGEXPORT void axConfig_set_SelectButtonIndex(axConfig* config, int selectButtonIndex);

IGEXPORT int axConfig_get_NavigateButtonIndex(axConfig* config);
IGEXPORT void axConfig_set_NavigateButtonIndex(axConfig* config, int navigateButtonIndex);

IGEXPORT int axConfig_get_ContextMenuButtonIndex(axConfig* config);
IGEXPORT void axConfig_set_ContextMenuButtonIndex(axConfig* config, int contextMenuButtonIndex);

struct axEditorContext;

IGEXPORT void axSetCurrentEditor(axEditorContext* ctx);
IGEXPORT axEditorContext* axGetCurrentEditor();
IGEXPORT axEditorContext* axCreateEditor(axConfig* config);
IGEXPORT void axDestroyEditor(axEditorContext* ctx);
IGEXPORT const axConfig* axGetConfig(axEditorContext* ctx);

IGEXPORT axStyle* axGetStyle();
IGEXPORT const char* axGetStyleColorName(axStyleColor colorIndex);

IGEXPORT void axPushStyleColor(axStyleColor colorIndex, axVec4* color);
IGEXPORT void axPopStyleColor(int count);

IGEXPORT void axPushStyleVar1(axStyleVar varIndex, float value);
IGEXPORT void axPushStyleVar2(axStyleVar varIndex, axVec2* value);
IGEXPORT void axPushStyleVar4(axStyleVar varIndex, axVec4* value);
IGEXPORT void axPopStyleVar(int count);

IGEXPORT void axBegin(const char* id, axVec2* size);
IGEXPORT void axEnd();

IGEXPORT void axBeginNode(axNodeId id);
IGEXPORT void axBeginPin(axPinId id, axPinKind kind);
IGEXPORT void axPinRect(axVec2* a, axVec2* b);
IGEXPORT void axPinPivotRect(axVec2* a, axVec2* b);
IGEXPORT void axPinPivotSize(axVec2* size);
IGEXPORT void axPinPivotScale(axVec2* scale);
IGEXPORT void axPinPivotAlignment(axVec2* alignment);
IGEXPORT void axEndPin();
IGEXPORT void axGroup(axVec2* size);
IGEXPORT void axEndNode();

IGEXPORT int axBeginGroupHint(axNodeId nodeId);
IGEXPORT void axGetGroupMin(axVec2 *gmin);
IGEXPORT void axGetGroupMax(axVec2 *gmax);
IGEXPORT void* axGetHintForegroundDrawList();
IGEXPORT void* axGetHintBackgroundDrawList();
IGEXPORT void axEndGroupHint();

IGEXPORT void* axGetNodeBackgroundDrawList(axNodeId nodeId);

IGEXPORT int axLink(axLinkId id, axPinId startPinId, axPinId endPinId, axVec4* color, float thickness);

IGEXPORT void axFlow(axLinkId linkId, axFlowDirection direction);

IGEXPORT int axBeginCreate(axVec4* color, float thickness);
IGEXPORT int axQueryNewLink(axPinId* startId, axPinId* endId);
IGEXPORT int axQueryNewLink_Styled(axPinId* startId, axPinId* endId, axVec4* color, float thickness);
IGEXPORT int axQueryNewNode(axPinId* pinId);
IGEXPORT int axQueryNewNode_Styled(axPinId* pinId, axVec4* color, float thickness);
IGEXPORT int axAcceptNewItem();
IGEXPORT int axAcceptNewItem_Styled(axVec4* color, float thickness);
IGEXPORT void axRejectNewItem();
IGEXPORT void axRejectNewItem_Styled(axVec4* color, float thickness);
IGEXPORT void axEndCreate();

IGEXPORT int axBeginDelete();
IGEXPORT int axQueryDeletedLink(axLinkId* linkId, axPinId* startId, axPinId* endId);
IGEXPORT int axQueryDeletedNode(axNodeId* nodeId);
IGEXPORT int axAcceptDeletedItem(int deleteDependencies);
IGEXPORT void axRejectDeletedItem();
IGEXPORT void axEndDelete();

IGEXPORT void axSetNodePosition(axNodeId nodeId, axVec2* editorPosition);
IGEXPORT void axSetGroupSize(axNodeId nodeId, axVec2* size);
IGEXPORT void axGetNodePosition(axNodeId nodeId, axVec2* pos);
IGEXPORT void axGetNodeSize(axNodeId nodeId, axVec2* sz);
IGEXPORT void axCenterNodeOnScreen(axNodeId nodeId);
IGEXPORT void axSetNodeZPosition(axNodeId nodeId, float z); // Sets node z position, nodes with higher value are drawn over nodes with lower value
IGEXPORT float axGetNodeZPosition(axNodeId nodeId); // Returns node z position, defaults is 0.0f

IGEXPORT void axRestoreNodeState(axNodeId nodeId);

IGEXPORT void axSuspend();
IGEXPORT void axResume();
IGEXPORT int axIsSuspended();

IGEXPORT int axIsActive();

IGEXPORT int axHasSelectionChanged();
IGEXPORT int axGetSelectedObjectCount();
IGEXPORT int axGetSelectedNodes(axNodeId* nodes, int size);
IGEXPORT int axGetSelectedLinks(axLinkId* links, int size);
IGEXPORT int axIsNodeSelected(axNodeId nodeId);
IGEXPORT int axIsLinkSelected(axLinkId linkId);
IGEXPORT void axClearSelection();
IGEXPORT void axSelectNode(axNodeId nodeId, int append);
IGEXPORT void axSelectLink(axLinkId linkId, int append);
IGEXPORT void axDeselectNode(axNodeId nodeId);
IGEXPORT void axDeselectLink(axLinkId linkId);

IGEXPORT int axDeleteNode(axNodeId nodeId);
IGEXPORT int axDeleteLink(axLinkId linkId);

IGEXPORT int axNodeHasAnyLinks(axNodeId nodeId); // Returns true if node has any link connected
IGEXPORT int axPinHasAnyLinks(axPinId pinId); // Return true if pin has any link connected
IGEXPORT int axNodeBreakLinks(axNodeId nodeId); // Break all links connected to this node
IGEXPORT int axPinBreakLinks(axPinId pinId); // Break all links connected to this pin

IGEXPORT void axNavigateToContent(float duration);
IGEXPORT void axNavigateToSelection(int zoomIn, float duration);

IGEXPORT int axShowNodeContextMenu(axNodeId* nodeId);
IGEXPORT int axShowPinContextMenu(axPinId* pinId);
IGEXPORT int axShowLinkContextMenu(axLinkId* linkId);
IGEXPORT int axShowBackgroundContextMenu();

IGEXPORT void axEnableShortcuts(int enable);
IGEXPORT int axAreShortcutsEnabled();

IGEXPORT int axBeginShortcut();
IGEXPORT int axAcceptCut();
IGEXPORT int axAcceptCopy();
IGEXPORT int axAcceptPaste();
IGEXPORT int axAcceptDuplicate();
IGEXPORT int axAcceptCreateNode();
IGEXPORT int axGetActionContextSize();
IGEXPORT int axGetActionContextNodes(axNodeId* nodes, int size);
IGEXPORT int axGetActionContextLinks(axLinkId* links, int size);
IGEXPORT void axEndShortcut();

IGEXPORT float axGetCurrentZoom();

IGEXPORT axNodeId axGetHoveredNode();
IGEXPORT axPinId axGetHoveredPin();
IGEXPORT axLinkId axGetHoveredLink();
IGEXPORT axNodeId axGetDoubleClickedNode();
IGEXPORT axPinId axGetDoubleClickedPin();
IGEXPORT axLinkId axGetDoubleClickedLink();
IGEXPORT int axIsBackgroundClicked();
IGEXPORT int axIsBackgroundDoubleClicked();
IGEXPORT int axGetBackgroundClickButtonIndex(); // -1 if none
IGEXPORT int axGetBackgroundDoubleClickButtonIndex(); // -1 if none

IGEXPORT int axGetLinkPins(axLinkId linkId, axPinId* startPinId, axPinId* endPinId); // pass nullptr if particular pin do not interest you

IGEXPORT int axPinHadAnyLinks(axPinId pinId);

IGEXPORT void axGetScreenSize(axVec2* size);
IGEXPORT void axScreenToCanvas(axVec2* pos, axVec2* canvas);
IGEXPORT void axCanvasToScreen(axVec2* pos, axVec2* screen);

IGEXPORT int axGetNodeCount();
IGEXPORT int axGetOrderedNodeIds(axNodeId* nodes, int size);

#ifdef __cplusplus
}
#endif
#endif
