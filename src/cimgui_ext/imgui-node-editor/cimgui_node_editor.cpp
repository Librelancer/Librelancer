#include "cimgui_ext.h"
#include "imgui_node_editor.h"
#include <stdlib.h>

#if defined(WIN32) || defined(__WIN32) || defined(__WIN32__)
#include <malloc.h>
#define ig_alloca _alloca
#else
#include <alloca.h>
#define ig_alloca alloca
#endif

namespace ed = ax::NodeEditor;

static ed::NodeId NodeFromPtr(axNodeId ptr) { return ed::NodeId(ptr); }

static axNodeId PtrFromNode(ed::NodeId nodeId) { return (axNodeId)nodeId.AsPointer(); }

static ed::LinkId LinkFromPtr(axLinkId ptr) { return ed::LinkId(ptr); }

static axLinkId PtrFromLink(ed::LinkId linkId) { return (axLinkId)linkId.AsPointer(); }

static ed::PinId PinFromPtr(axPinId ptr) { return ed::PinId(ptr); }

static axPinId PtrFromPin(ed::PinId pinId) { return (axPinId)pinId.AsPointer(); }

static axVec2 FromImVec2(ImVec2 vec) { axVec2 ret; ret.x = vec.x; ret.y = vec.y; return ret; }

static ImVec2 ToImVec2(axVec2 vec) { return ImVec2(vec.x, vec.y); }

static ImVec4 ToImVec4(axVec4 vec) { return ImVec4(vec.x, vec.y, vec.z, vec.w); }

typedef struct {
    axConfigSaveSettings saveSettings;
    axConfigLoadSettings loadSettings;
    axConfigSaveNodeSettings saveNodeSettings;
    axConfigLoadNodeSettings loadNodeSettings;
    axConfigSession beginSaveSession;
    axConfigSession endSaveSession;
    axNodeDraggedCallback nodeDraggedHook;
    axNodeResizedCallback nodeResizedHook;
    void* userPointer;
} internalUserData;

#define INTERNAL_DATA internalUserData *internalData = (internalUserData*)userPointer;
static bool internal_saveSettings(const char* data, size_t size, ed::SaveReasonFlags reason, void* userPointer)
{
    INTERNAL_DATA
    return internalData->saveSettings(data, size, (axSaveReasonFlags)reason, internalData->userPointer) != 0;
}

static size_t internal_loadSettings(char* data, void* userPointer)
{
    INTERNAL_DATA
    return internalData->loadSettings(data, internalData->userPointer);
}

static bool internal_saveNodeSettings(ed::NodeId nodeId, const char* data, size_t size, ed::SaveReasonFlags reason, void* userPointer)
{
    INTERNAL_DATA
    return internalData->saveNodeSettings(PtrFromNode(nodeId), data, size, (axSaveReasonFlags)reason, internalData->userPointer) != 0;
}

static size_t internal_loadNodeSettings(ed::NodeId nodeId, char* data, void* userPointer)
{
    INTERNAL_DATA
    return internalData->loadNodeSettings(PtrFromNode(nodeId), data, internalData->userPointer);
}

static void internal_beginSaveSession(void* userPointer)
{
    INTERNAL_DATA
    internalData->beginSaveSession(internalData->userPointer);
}

static void internal_endSaveSession(void* userPointer)
{
    INTERNAL_DATA
    internalData->endSaveSession(internalData->userPointer);
}

static void internal_nodeDraggedHook(ed::NodeId nodeId, float oldX, float oldY, float newX, float newY, void *userPointer)
{
    INTERNAL_DATA;
    internalData->nodeDraggedHook(PtrFromNode(nodeId), oldX, oldY, newX, newY, internalData->userPointer);
}

static void internal_nodeResizedHook(ed::NodeId nodeId, ed::ResizeCallbackData* data, void *userPointer)
{
    INTERNAL_DATA;
    internalData->nodeResizedHook(PtrFromNode(nodeId), (axResizeCallbackData*)data, internalData->userPointer);
}
#undef INTERNAL_DATA


CIMGUI_API axConfig* axConfigNew()
{
    internalUserData *internalData = (internalUserData*)malloc(sizeof(internalUserData));
    memset(internalData, 0, sizeof(internalUserData));
    ed::Config *cfg = new ed::Config();
    cfg->UserPointer = (void*)internalData;
    return (axConfig*)cfg;
}

CIMGUI_API void axConfigFree(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    free(cfg->UserPointer);
    delete cfg;
}

CIMGUI_API const char* axConfig_get_SettingsFile(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    return cfg->SettingsFile;
}

CIMGUI_API void axConfig_set_SettingsFile(axConfig* config, const char *settingsFile)
{
    ed::Config *cfg = (ed::Config*)config;
    cfg->SettingsFile = settingsFile;
}

CIMGUI_API void* axConfig_get_UserPointer(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    return internalData->userPointer;
}

CIMGUI_API void axConfig_set_UserPointer(axConfig* config, void* userPointer)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    internalData->userPointer = userPointer;
}

CIMGUI_API axConfigSession axConfig_get_BeginSaveSession(axConfig *config)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    return internalData->beginSaveSession;
}

CIMGUI_API void axConfig_set_BeginSaveSession(axConfig *config, axConfigSession beginSaveSession)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    internalData->beginSaveSession = beginSaveSession;
    cfg->BeginSaveSession = beginSaveSession ? &internal_beginSaveSession : nullptr;
}

CIMGUI_API axConfigSession axConfig_get_EndSaveSession(axConfig *config)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    return internalData->endSaveSession;
}

CIMGUI_API void axConfig_set_EndSaveSession(axConfig *config, axConfigSession endSaveSession)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    internalData->endSaveSession = endSaveSession;
    cfg->EndSaveSession = endSaveSession ? &internal_endSaveSession : nullptr;
}

CIMGUI_API axConfigSaveSettings axConfig_get_SaveSettings(axConfig *config)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    return internalData->saveSettings;
}

CIMGUI_API void axConfig_set_SaveSettings(axConfig *config, axConfigSaveSettings saveSettings)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    internalData->saveSettings = saveSettings;
    cfg->SaveSettings = saveSettings ? &internal_saveSettings : nullptr;
}

CIMGUI_API axConfigLoadSettings axConfig_get_LoadSettings(axConfig *config)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    return internalData->loadSettings;
}

CIMGUI_API void axConfig_set_LoadSettings(axConfig *config, axConfigLoadSettings loadSettings)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    internalData->loadSettings = loadSettings;
    cfg->LoadSettings = loadSettings ? &internal_loadSettings : nullptr;
}

CIMGUI_API axConfigSaveNodeSettings axConfig_get_SaveNodeSettings(axConfig *config)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    return internalData->saveNodeSettings;
}
CIMGUI_API void axConfig_set_SaveNodeSettings(axConfig *config, axConfigSaveNodeSettings saveNodeSettings)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    internalData->saveNodeSettings = saveNodeSettings;
    cfg->SaveNodeSettings = saveNodeSettings ? &internal_saveNodeSettings : nullptr;
}

CIMGUI_API axConfigLoadNodeSettings axConfig_get_LoadNodeSettings(axConfig *config)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    return internalData->loadNodeSettings;
}
CIMGUI_API void axConfig_set_LoadNodeSettings(axConfig *config, axConfigLoadNodeSettings loadNodeSettings)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    internalData->loadNodeSettings = loadNodeSettings;
    cfg->LoadNodeSettings = loadNodeSettings ? &internal_loadNodeSettings : nullptr;
}

CIMGUI_API axNodeDraggedCallback axConfig_get_NodeDraggedHook(axConfig *config)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    return internalData->nodeDraggedHook;
}

CIMGUI_API void axConfig_set_NodeDraggedHook(axConfig *config, axNodeDraggedCallback nodeDraggedHook)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    internalData->nodeDraggedHook = nodeDraggedHook;
    cfg->NodeDraggedHook = nodeDraggedHook ? &internal_nodeDraggedHook : nullptr;
}

CIMGUI_API void axConfig_set_NodeResizedHook(axConfig *config, axNodeResizedCallback nodeResizedHook)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    internalData->nodeResizedHook = nodeResizedHook;
    cfg->NodeResizedHook = nodeResizedHook ? &internal_nodeResizedHook : nullptr;
}

CIMGUI_API axCanvasSizeMode axConfig_get_CanvasSizeMode(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    return (axCanvasSizeMode)cfg->CanvasSizeMode;
}

CIMGUI_API void axConfig_set_CanvasSizeMode(axConfig* config, axCanvasSizeMode canvasSizeMode)
{
    ed::Config *cfg = (ed::Config*)config;
    cfg->CanvasSizeMode = (ed::CanvasSizeMode)canvasSizeMode;
}

CIMGUI_API int axConfig_get_DragButtonIndex(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    return cfg->DragButtonIndex;
}

CIMGUI_API void axConfig_set_DragButtonIndex(axConfig* config, int dragButtonIndex)
{
    ed::Config *cfg = (ed::Config*)config;
    cfg->DragButtonIndex = dragButtonIndex;
}

CIMGUI_API int axConfig_get_SelectButtonIndex(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    return cfg->SelectButtonIndex;
}

CIMGUI_API void axConfig_set_SelectButtonIndex(axConfig* config, int selectButtonIndex)
{
    ed::Config *cfg = (ed::Config*)config;
    cfg->SelectButtonIndex = selectButtonIndex;
}

CIMGUI_API int axConfig_get_NavigateButtonIndex(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    return cfg->NavigateButtonIndex;
}

CIMGUI_API void axConfig_set_NavigateButtonIndex(axConfig* config, int navigateButtonIndex)
{
    ed::Config *cfg = (ed::Config*)config;
    cfg->NavigateButtonIndex = navigateButtonIndex;
}

CIMGUI_API int axConfig_get_ContextMenuButtonIndex(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    return cfg->NavigateButtonIndex;
}

CIMGUI_API void axConfig_set_ContextMenuButtonIndex(axConfig* config, int contextMenuButtonIndex)
{
    ed::Config *cfg = (ed::Config*)config;
    cfg->ContextMenuButtonIndex = contextMenuButtonIndex;
}

CIMGUI_API int axConfig_get_EnableSmoothZoom(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    return cfg->EnableSmoothZoom ? 1 : 0;
}

CIMGUI_API void axConfig_set_EnableSmoothZoom(axConfig* config, int smoothZoom)
{
    ed::Config *cfg = (ed::Config*)config;
    cfg->EnableSmoothZoom = smoothZoom != 0;
}

CIMGUI_API float axConfig_get_SmoothZoomPower(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    return cfg->SmoothZoomPower;
}

CIMGUI_API void axConfig_set_SmoothZoomPower(axConfig* config, float smoothZoomPower)
{
    ed::Config *cfg = (ed::Config*)config;
    cfg->SmoothZoomPower = smoothZoomPower;
}

//Editor Context
CIMGUI_API void axSetCurrentEditor(axEditorContext* ctx)
{
    ed::SetCurrentEditor((ed::EditorContext*)ctx);
}
CIMGUI_API axEditorContext* axGetCurrentEditor()
{
    return (axEditorContext*)ed::GetCurrentEditor();
}

CIMGUI_API axEditorContext* axCreateEditor(axConfig* config)
{
    return (axEditorContext*)ed::CreateEditor((ed::Config*)config);
}

CIMGUI_API void axDestroyEditor(axEditorContext* ctx)
{
    ed::DestroyEditor((ed::EditorContext*)ctx);
}

CIMGUI_API const axConfig* axGetConfig(axEditorContext* ctx)
{
    return (axConfig*)&ed::GetConfig((ed::EditorContext*)ctx);
}

//Style

CIMGUI_API void axStyle_Init(axStyle *style)
{
    ed::Style defaultStyle{};
    *style = *(axStyle*)&defaultStyle;
}

CIMGUI_API axStyle* axGetStyle()
{
    return (axStyle*)&ed::GetStyle();
}

CIMGUI_API const char* axGetStyleColorName(axStyleColor colorIndex)
{
    return ed::GetStyleColorName((ed::StyleColor)colorIndex);
}

CIMGUI_API void axPushStyleColor(axStyleColor colorIndex, axVec4* color)
{
    ed::PushStyleColor((ed::StyleColor)colorIndex, ToImVec4(*color));
}

CIMGUI_API void axPopStyleColor(int count)
{
    ed::PopStyleColor(count);
}

CIMGUI_API void axPushStyleVar1(axStyleVar varIndex, float value)
{
    ed::PushStyleVar((ed::StyleVar)varIndex, value);
}
CIMGUI_API void axPushStyleVar2(axStyleVar varIndex, axVec2* value)
{
    ed::PushStyleVar((ed::StyleVar)varIndex, ToImVec2(*value));
}
CIMGUI_API void axPushStyleVar4(axStyleVar varIndex, axVec4* value)
{
    ed::PushStyleVar((ed::StyleVar)varIndex, ToImVec4(*value));
}
CIMGUI_API void axPopStyleVar(int count)
{
    ed::PopStyleVar(count);
}

CIMGUI_API void axBegin(const char* id, axVec2* size)
{
    ed::Begin(id, ToImVec2(*size));
}

CIMGUI_API void axEnd()
{
    ed::End();
}

CIMGUI_API void axBeginNode(axNodeId id)
{
    ed::BeginNode(NodeFromPtr(id));
}

CIMGUI_API void axBeginPin(axPinId id, axPinKind kind)
{
    ed::BeginPin(PinFromPtr(id), (ed::PinKind)kind);
}

CIMGUI_API void axPinRect(axVec2* a, axVec2* b)
{
    ed::PinRect(ToImVec2(*a), ToImVec2(*b));
}

CIMGUI_API void axPinPivotRect(axVec2* a, axVec2* b)
{
    ed::PinPivotRect(ToImVec2(*a), ToImVec2(*b));
}

CIMGUI_API void axPinPivotSize(axVec2* size)
{
    ed::PinPivotSize(ToImVec2(*size));
}

CIMGUI_API void axPinPivotScale(axVec2* scale)
{
    ed::PinPivotScale(ToImVec2(*scale));
}

CIMGUI_API void axPinPivotAlignment(axVec2* alignment)
{
    ed::PinPivotAlignment(ToImVec2(*alignment));
}

CIMGUI_API void axEndPin()
{
    ed::EndPin();
}

CIMGUI_API void axGroup(axVec2* size)
{
    ed::Group(ToImVec2(*size));
}

CIMGUI_API void axEndNode()
{
    ed::EndNode();
}

CIMGUI_API int axBeginGroupHint(axNodeId nodeId)
{
    return ed::BeginGroupHint(NodeFromPtr(nodeId)) ? 1 : 0;
}

CIMGUI_API void axGetGroupMin(axVec2 *gmin)
{
    *gmin = FromImVec2(ed::GetGroupMin());
}

CIMGUI_API void axGetGroupMax(axVec2 *gmax)
{
    *gmax = FromImVec2(ed::GetGroupMax());
}

CIMGUI_API void* axGetHintForegroundDrawList()
{
    return (void*)ed::GetHintForegroundDrawList();
}

CIMGUI_API void* axGetHintBackgroundDrawList()
{
    return (void*)ed::GetHintBackgroundDrawList();
}

CIMGUI_API void axEndGroupHint()
{
    ed::EndGroupHint();
}

CIMGUI_API void* axGetNodeBackgroundDrawList(axNodeId nodeId)
{
    return (void*)ed::GetNodeBackgroundDrawList(NodeFromPtr(nodeId));
}

CIMGUI_API int axLink(axLinkId id, axPinId startPinId, axPinId endPinId, axVec4* color, float thickness)
{
    return ed::Link(LinkFromPtr(id), PinFromPtr(startPinId), PinFromPtr(endPinId), ToImVec4(*color), thickness) ? 1 : 0;
}


CIMGUI_API void axFlow(axLinkId linkId, axFlowDirection direction)
{
    ed::Flow(LinkFromPtr(linkId), (ed::FlowDirection)direction);
}

CIMGUI_API int axBeginCreate(axVec4* color, float thickness)
{
    return ed::BeginCreate(ToImVec4(*color), thickness);
}

CIMGUI_API int axQueryNewLink(axPinId* startId, axPinId* endId)
{
    ed::PinId sp{};
    ed::PinId ep{};
    int retval = ed::QueryNewLink(&sp, &ep) ? 1 : 0;
    if(startId) *startId = PtrFromPin(sp);
    if(endId) *endId = PtrFromPin(ep);
    return retval;
}

CIMGUI_API int axQueryNewLink_Styled(axPinId* startId, axPinId* endId, axVec4* color, float thickness)
{
    ed::PinId sp{};
    ed::PinId ep{};
    int retval = ed::QueryNewLink(&sp, &ep, ToImVec4(*color), thickness) ? 1 : 0;
    if(startId) *startId = PtrFromPin(sp);
    if(endId) *endId = PtrFromPin(ep);
    return retval;
}

CIMGUI_API int axQueryNewNode(axPinId* pinId)
{
    ed::PinId p{};
    int retval = ed::QueryNewNode(&p);
    if(pinId) *pinId = PtrFromPin(p);
    return retval;
}

CIMGUI_API int axQueryNewNode_Styled(axPinId* pinId, axVec4* color, float thickness)
{
    ed::PinId p{};
    int retval = ed::QueryNewNode(&p, ToImVec4(*color), thickness);
    if (pinId) *pinId = PtrFromPin(p);
    return retval;
}

CIMGUI_API int axAcceptNewItem()
{
    return ed::AcceptNewItem() ? 1 : 0;
}
CIMGUI_API int axAcceptNewItem_Styled(axVec4* color, float thickness)
{
    return ed::AcceptNewItem(ToImVec4(*color), thickness) ? 1 : 0;
}

CIMGUI_API void axRejectNewItem()
{
    ed::RejectNewItem();
}

CIMGUI_API void axRejectNewItem_Styled(axVec4* color, float thickness)
{
    ed::RejectNewItem(ToImVec4(*color), thickness);
}

CIMGUI_API void axEndCreate()
{
    ed::EndCreate();
}

CIMGUI_API int axBeginDelete()
{
    return ed::BeginDelete() ? 1 : 0;
}

CIMGUI_API int axQueryDeletedLink(axLinkId* linkId, axPinId* startId, axPinId* endId)
{
   ed::LinkId ld{};
   ed::PinId sp{};
   ed::PinId ep{};
   int retval = ed::QueryDeletedLink(linkId ? &ld: nullptr, startId ? &sp : nullptr, endId? &ep : nullptr);
   if(linkId) *linkId = PtrFromLink(ld);
   if(startId) *startId = PtrFromPin(sp);
   if(endId) *endId = PtrFromPin(ep);
   return retval;
}

CIMGUI_API int axQueryDeletedNode(axNodeId* nodeId)
{
    ed::NodeId nd{};
    int retval = ed::QueryDeletedNode(&nd) ? 1 : 0;
    if(nodeId) *nodeId = PtrFromNode(nd);
    return retval;
}
CIMGUI_API int axAcceptDeletedItem(int deleteDependencies)
{
    return ed::AcceptDeletedItem(deleteDependencies != 0) ? 1 : 0;
}
CIMGUI_API void axRejectDeletedItem()
{
    ed::RejectDeletedItem();
}
CIMGUI_API void axEndDelete()
{
    ed::EndDelete();
}

CIMGUI_API void axSetNodePosition(axNodeId nodeId, axVec2* editorPosition)
{
    ed::SetNodePosition(NodeFromPtr(nodeId), ToImVec2(*editorPosition));
}

CIMGUI_API void axSetGroupSize(axNodeId nodeId, axVec2* size)
{
    ed::SetGroupSize(NodeFromPtr(nodeId), ToImVec2(*size));
}
CIMGUI_API void axGetNodePosition(axNodeId nodeId, axVec2* pos)
{
    *pos = FromImVec2(ed::GetNodePosition(NodeFromPtr(nodeId)));
}
CIMGUI_API void axGetNodeSize(axNodeId nodeId, axVec2* sz)
{
    *sz = FromImVec2(ed::GetNodeSize(NodeFromPtr(nodeId)));
}
CIMGUI_API void axCenterNodeOnScreen(axNodeId nodeId)
{
    ed::CenterNodeOnScreen(NodeFromPtr(nodeId));
}
CIMGUI_API void axSetNodeZPosition(axNodeId nodeId, float z)
{
    ed::SetNodeZPosition(NodeFromPtr(nodeId), z);
}

CIMGUI_API float axGetNodeZPosition(axNodeId nodeId)
{
    return ed::GetNodeZPosition(NodeFromPtr(nodeId));
}
CIMGUI_API void axSuspend()
{
    ed::Suspend();
}

CIMGUI_API void axResume()
{
    ed::Resume();
}

CIMGUI_API int axIsSuspended()
{
    return ed::IsSuspended() ? 1 : 0;
}

CIMGUI_API int axIsActive()
{
    return ed::IsActive() ? 1 : 0;
}

CIMGUI_API int axHasSelectionChanged()
{
    return ed::HasSelectionChanged() ? 1 : 0;
}
CIMGUI_API int axGetSelectedObjectCount()
{
    return ed::GetSelectedObjectCount();
}

CIMGUI_API int axGetSelectedNodes(axNodeId* nodes, int size)
{
    ed::NodeId* ids = (ed::NodeId*)ig_alloca(sizeof(ed::NodeId) * size);
    int retval = ed::GetSelectedNodes(ids, size);
    for(int i = 0; i < size && i < retval; i++)
        nodes[i] = PtrFromNode(ids[i]);
    return retval;
}
CIMGUI_API int axGetSelectedLinks(axLinkId* links, int size)
{
    ed::LinkId* ids = (ed::LinkId*)ig_alloca(sizeof(ed::LinkId) * size);
    int retval = ed::GetSelectedLinks(ids, size);
    for(int i = 0; i < size && i < retval; i++)
        links[i] = PtrFromLink(ids[i]);
    return retval;
}
CIMGUI_API int axIsNodeSelected(axNodeId nodeId)
{
    return ed::IsNodeSelected(NodeFromPtr(nodeId)) ? 1 : 0;
}
CIMGUI_API int axIsLinkSelected(axLinkId linkId)
{
    return ed::IsLinkSelected(LinkFromPtr(linkId)) ? 1 : 0;
}
CIMGUI_API void axClearSelection()
{
    ed::ClearSelection();
}
CIMGUI_API void axSelectNode(axNodeId nodeId, int append)
{
    ed::SelectNode(NodeFromPtr(nodeId), append != 0);
}
CIMGUI_API void axSelectLink(axLinkId linkId, int append)
{
    ed::SelectLink(LinkFromPtr(linkId), append != 0);
}
CIMGUI_API void axDeselectNode(axNodeId nodeId)
{
    ed::DeselectNode(NodeFromPtr(nodeId));
}

CIMGUI_API void axDeselectLink(axLinkId linkId)
{
    ed::DeselectLink(LinkFromPtr(linkId));
}

CIMGUI_API int axDeleteNode(axNodeId nodeId)
{
    return ed::DeleteNode(NodeFromPtr(nodeId)) ? 1 : 0;
}

CIMGUI_API int axDeleteLink(axLinkId linkId)
{
    return ed::DeleteLink(LinkFromPtr(linkId)) ? 1 : 0;
}



CIMGUI_API int axNodeHasAnyLinks(axNodeId nodeId) // Returns true if node has any link connected
{
    return ed::HasAnyLinks(NodeFromPtr(nodeId)) ? 1 : 0;
}
CIMGUI_API int axPinHasAnyLinks(axPinId pinId) // Return true if pin has any link connected
{
    return ed::HasAnyLinks(PinFromPtr(pinId)) ? 1 : 0;
}
CIMGUI_API int axNodeBreakLinks(axNodeId nodeId) // Break all links connected to this node
{
    return ed::BreakLinks(NodeFromPtr(nodeId));
}
CIMGUI_API int axPinBreakLinks(axPinId pinId) // Break all links connected to this pin
{
    return ed::BreakLinks(PinFromPtr(pinId));
}

CIMGUI_API void axNavigateToContent(float duration)
{
    ed::NavigateToContent(duration);
}
CIMGUI_API void axNavigateToSelection(int zoomIn, float duration)
{
    ed::NavigateToSelection(zoomIn != 0, duration);
}

CIMGUI_API void axEnableShortcuts(int enable)
{
    ed::EnableShortcuts(enable != 0);
}

CIMGUI_API int axAreShortcutsEnabled()
{
    return ed::AreShortcutsEnabled() ? 1 : 0;
}

CIMGUI_API int axShowNodeContextMenu(axNodeId* nodeId)
{
    ed::NodeId n{};
    bool rv = ed::ShowNodeContextMenu(&n);
    *nodeId = PtrFromNode(n);
    return rv ? 1 : 0;
}

CIMGUI_API int axShowPinContextMenu(axPinId* pinId)
{
    ed::PinId p{};
    bool rv = ed::ShowPinContextMenu(&p);
    *pinId = PtrFromPin(p);
    return rv ? 1 : 0;
}

CIMGUI_API int axShowLinkContextMenu(axLinkId* linkId)
{
    ed::LinkId l{};
    bool rv = ed::ShowLinkContextMenu(&l);
    *linkId = PtrFromLink(l);
    return rv ? 1 : 0;
}

CIMGUI_API int axShowBackgroundContextMenu()
{
    return ed::ShowBackgroundContextMenu() ? 1 : 0;
}

CIMGUI_API int axBeginShortcut()
{
    return ed::BeginShortcut() ? 1 : 0;
}
CIMGUI_API int axAcceptCut()
{
    return ed::AcceptCut() ? 1 : 0;
}
CIMGUI_API int axAcceptCopy()
{
    return ed::AcceptCopy() ? 1 : 0;
}
CIMGUI_API int axAcceptPaste()
{
    return ed::AcceptPaste() ? 1 : 0;
}
CIMGUI_API int axAcceptDuplicate()
{
    return ed::AcceptDuplicate() ? 1 : 0;
}
CIMGUI_API int axAcceptCreateNode()
{
    return ed::AcceptCreateNode() ? 1 : 0;
}
CIMGUI_API int axGetActionContextSize()
{
    return ed::GetActionContextSize();
}
CIMGUI_API int axGetActionContextNodes(axNodeId* nodes, int size)
{
    ed::NodeId* ids = (ed::NodeId*)ig_alloca(sizeof(ed::NodeId) * size);
    int retval = ed::GetActionContextNodes(ids, size);
    for(int i = 0; i < size && i < retval; i++)
        nodes[i] = PtrFromNode(ids[i]);
    return retval;
}

CIMGUI_API int axGetActionContextLinks(axLinkId* links, int size)
{
    ed::LinkId* ids = (ed::LinkId*)ig_alloca(sizeof(ed::LinkId) * size);
    int retval = ed::GetActionContextLinks(ids, size);
    for(int i = 0; i < size && i < retval; i++)
        links[i] = PtrFromLink(ids[i]);
    return retval;
}

CIMGUI_API void axEndShortcut()
{
    ed::EndShortcut();
}

CIMGUI_API float axGetCurrentZoom()
{
    return ed::GetCurrentZoom();
}

CIMGUI_API axNodeId axGetHoveredNode()
{
    return PtrFromNode(ed::GetHoveredNode());
}

CIMGUI_API axPinId axGetHoveredPin()
{
    return PtrFromPin(ed::GetHoveredPin());
}

CIMGUI_API axLinkId axGetHoveredLink()
{
    return PtrFromLink(ed::GetHoveredLink());
}

CIMGUI_API axNodeId axGetDoubleClickedNode()
{
    return PtrFromNode(ed::GetDoubleClickedNode());
}

CIMGUI_API axPinId axGetDoubleClickedPin()
{
    return PtrFromPin(ed::GetDoubleClickedPin());
}

CIMGUI_API axLinkId axGetDoubleClickedLink()
{
    return PtrFromLink(ed::GetDoubleClickedLink());
}

CIMGUI_API int axIsBackgroundClicked()
{
    return ed::IsBackgroundClicked() ? 1 : 0;
}

CIMGUI_API int axIsBackgroundDoubleClicked()
{
    return ed::IsBackgroundDoubleClicked() ? 1 : 0;
}

CIMGUI_API int axGetBackgroundClickButtonIndex() // -1 if none
{
    return (int)ed::GetBackgroundClickButtonIndex();
}

CIMGUI_API int axGetBackgroundDoubleClickButtonIndex()
{
    return (int)ed::GetBackgroundDoubleClickButtonIndex();
}

CIMGUI_API int axGetLinkPins(axLinkId linkId, axPinId* startPinId, axPinId* endPinId)
{
    ed::PinId sp {};
    ed::PinId ep {};
    int retval = ed::GetLinkPins(LinkFromPtr(linkId), startPinId ? &sp : nullptr, endPinId ? &ep : nullptr) ? 1 : 0;
    if(startPinId) *startPinId = PtrFromPin(sp);
    if(endPinId) *endPinId = PtrFromPin(ep);
    return retval;
}

CIMGUI_API int axPinHadAnyLinks(axPinId pinId)
{
    return ed::PinHadAnyLinks(PinFromPtr(pinId)) ? 1 : 0;
}


CIMGUI_API void axGetScreenSize(axVec2* size)
{
    *size = FromImVec2(ed::GetScreenSize());
}

CIMGUI_API void axScreenToCanvas(axVec2* pos, axVec2* canvas)
{
    *canvas = FromImVec2(ed::ScreenToCanvas(ToImVec2(*pos)));
}

CIMGUI_API void axCanvasToScreen(axVec2* pos, axVec2* screen)
{
    *screen = FromImVec2(ed::CanvasToScreen(ToImVec2(*pos)));
}

CIMGUI_API int axGetNodeCount()
{
    return ed::GetNodeCount();
}

CIMGUI_API int axGetOrderedNodeIds(axNodeId* nodes, int size)
{
    ed::NodeId* ids = (ed::NodeId*)ig_alloca(sizeof(ed::NodeId) * size);
    int retval = ed::GetOrderedNodeIds(ids, size);
    for(int i = 0; i < size && i < retval; i++)
        nodes[i] = PtrFromNode(ids[i]);
    return retval;
}
