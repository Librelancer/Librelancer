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
#undef INTERNAL_DATA


IGEXPORT axConfig* axConfigNew()
{
    internalUserData *internalData = (internalUserData*)malloc(sizeof(internalUserData));
    memset(internalData, 0, sizeof(internalUserData));
    ed::Config *cfg = new ed::Config();
    cfg->UserPointer = (void*)internalData;
    return (axConfig*)cfg;
}

IGEXPORT void axConfigFree(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    free(cfg->UserPointer);
    delete cfg;
}

IGEXPORT const char* axConfig_get_SettingsFile(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    return cfg->SettingsFile;
}

IGEXPORT void axConfig_set_SettingsFile(axConfig* config, const char *settingsFile)
{
    ed::Config *cfg = (ed::Config*)config;
    cfg->SettingsFile = settingsFile;
}

IGEXPORT void* axConfig_get_UserPointer(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    return internalData->userPointer;
}

IGEXPORT void axConfig_set_UserPointer(axConfig* config, void* userPointer)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    internalData->userPointer = userPointer;
}

IGEXPORT axConfigSession axConfig_get_BeginSaveSession(axConfig *config)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    return internalData->beginSaveSession;
}

IGEXPORT void axConfig_set_BeginSaveSession(axConfig *config, axConfigSession beginSaveSession)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    internalData->beginSaveSession = beginSaveSession;
    cfg->BeginSaveSession = beginSaveSession ? &internal_beginSaveSession : nullptr;
}

IGEXPORT axConfigSession axConfig_get_EndSaveSession(axConfig *config)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    return internalData->endSaveSession;
}

IGEXPORT void axConfig_set_EndSaveSession(axConfig *config, axConfigSession endSaveSession)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    internalData->endSaveSession = endSaveSession;
    cfg->EndSaveSession = endSaveSession ? &internal_endSaveSession : nullptr;
}

IGEXPORT axConfigSaveSettings axConfig_get_SaveSettings(axConfig *config)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    return internalData->saveSettings;
}

IGEXPORT void axConfig_set_SaveSettings(axConfig *config, axConfigSaveSettings saveSettings)\
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    internalData->saveSettings = saveSettings;
    cfg->SaveSettings = saveSettings ? &internal_saveSettings : nullptr;
}

IGEXPORT axConfigLoadSettings axConfig_get_LoadSettings(axConfig *config)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    return internalData->loadSettings;
}

IGEXPORT void axConfig_set_LoadSettings(axConfig *config, axConfigLoadSettings loadSettings)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    internalData->loadSettings = loadSettings;
    cfg->LoadSettings = loadSettings ? &internal_loadSettings : nullptr;
}

IGEXPORT axConfigSaveNodeSettings axConfig_get_SaveNodeSettings(axConfig *config)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    return internalData->saveNodeSettings;
}
IGEXPORT void axConfig_set_SaveNodeSettings(axConfig *config, axConfigSaveNodeSettings saveNodeSettings)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    internalData->saveNodeSettings = saveNodeSettings;
    cfg->SaveNodeSettings = saveNodeSettings ? &internal_saveNodeSettings : nullptr;
}

IGEXPORT axConfigLoadNodeSettings axConfig_get_LoadNodeSettings(axConfig *config)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    return internalData->loadNodeSettings;
}
IGEXPORT void axConfig_set_LoadNodeSettings(axConfig *config, axConfigLoadNodeSettings loadNodeSettings)
{
    ed::Config *cfg = (ed::Config*)config;
    internalUserData *internalData = (internalUserData*)cfg->UserPointer;
    internalData->loadNodeSettings = loadNodeSettings;
    cfg->LoadNodeSettings = loadNodeSettings ? &internal_loadNodeSettings : nullptr;
}


IGEXPORT axCanvasSizeMode axConfig_get_CanvasSizeMode(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    return (axCanvasSizeMode)cfg->CanvasSizeMode;
}

IGEXPORT void axConfig_set_CanvasSizeMode(axConfig* config, axCanvasSizeMode canvasSizeMode)
{
    ed::Config *cfg = (ed::Config*)config;
    cfg->CanvasSizeMode = (ed::CanvasSizeMode)canvasSizeMode;
}

IGEXPORT int axConfig_get_DragButtonIndex(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    return cfg->DragButtonIndex;
}

IGEXPORT void axConfig_set_DragButtonIndex(axConfig* config, int dragButtonIndex)
{
    ed::Config *cfg = (ed::Config*)config;
    cfg->DragButtonIndex = dragButtonIndex;
}

IGEXPORT int axConfig_get_SelectButtonIndex(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    return cfg->SelectButtonIndex;
}

IGEXPORT void axConfig_set_SelectButtonIndex(axConfig* config, int selectButtonIndex)
{
    ed::Config *cfg = (ed::Config*)config;
    cfg->SelectButtonIndex = selectButtonIndex;
}

IGEXPORT int axConfig_get_NavigateButtonIndex(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    return cfg->NavigateButtonIndex;
}

IGEXPORT void axConfig_set_NavigateButtonIndex(axConfig* config, int navigateButtonIndex)
{
    ed::Config *cfg = (ed::Config*)config;
    cfg->NavigateButtonIndex = navigateButtonIndex;
}

IGEXPORT int axConfig_get_ContextMenuButtonIndex(axConfig* config)
{
    ed::Config *cfg = (ed::Config*)config;
    return cfg->NavigateButtonIndex;
}

IGEXPORT void axConfig_set_ContextMenuButtonIndex(axConfig* config, int contextMenuButtonIndex)
{
    ed::Config *cfg = (ed::Config*)config;
    cfg->ContextMenuButtonIndex = contextMenuButtonIndex;
}

//Editor Context
IGEXPORT void axSetCurrentEditor(axEditorContext* ctx)
{
    ed::SetCurrentEditor((ed::EditorContext*)ctx);
}
IGEXPORT axEditorContext* axGetCurrentEditor()
{
    return (axEditorContext*)ed::GetCurrentEditor();
}

IGEXPORT axEditorContext* axCreateEditor(axConfig* config)
{
    return (axEditorContext*)ed::CreateEditor((ed::Config*)config);
}

IGEXPORT void axDestroyEditor(axEditorContext* ctx)
{
    ed::DestroyEditor((ed::EditorContext*)ctx);
}

IGEXPORT const axConfig* axGetConfig(axEditorContext* ctx)
{
    return (axConfig*)&ed::GetConfig((ed::EditorContext*)ctx);
}

//Style

IGEXPORT void axStyle_Init(axStyle *style)
{
    ed::Style defaultStyle{};
    *style = *(axStyle*)&defaultStyle;
}

IGEXPORT axStyle* axGetStyle()
{
    return (axStyle*)&ed::GetStyle();
}

IGEXPORT const char* axGetStyleColorName(axStyleColor colorIndex)
{
    return ed::GetStyleColorName((ed::StyleColor)colorIndex);
}

IGEXPORT void axPushStyleColor(axStyleColor colorIndex, axVec4* color)
{
    ed::PushStyleColor((ed::StyleColor)colorIndex, ToImVec4(*color));
}

IGEXPORT void axPopStyleColor(int count)
{
    ed::PopStyleColor(count);
}

IGEXPORT void axPushStyleVar1(axStyleVar varIndex, float value)
{
    ed::PushStyleVar((ed::StyleVar)varIndex, value);
}
IGEXPORT void axPushStyleVar2(axStyleVar varIndex, axVec2* value)
{
    ed::PushStyleVar((ed::StyleVar)varIndex, ToImVec2(*value));
}
IGEXPORT void axPushStyleVar4(axStyleVar varIndex, axVec4* value)
{
    ed::PushStyleVar((ed::StyleVar)varIndex, ToImVec4(*value));
}
IGEXPORT void axPopStyleVar(int count)
{
    ed::PopStyleVar(count);
}

IGEXPORT void axBegin(const char* id, axVec2* size)
{
    ed::Begin(id, ToImVec2(*size));
}

IGEXPORT void axEnd()
{
    ed::End();
}

IGEXPORT void axBeginNode(axNodeId id)
{
    ed::BeginNode(NodeFromPtr(id));
}

IGEXPORT void axBeginPin(axPinId id, axPinKind kind)
{
    ed::BeginPin(PinFromPtr(id), (ed::PinKind)kind);
}

IGEXPORT void axPinRect(axVec2* a, axVec2* b)
{
    ed::PinRect(ToImVec2(*a), ToImVec2(*b));
}

IGEXPORT void axPinPivotRect(axVec2* a, axVec2* b)
{
    ed::PinPivotRect(ToImVec2(*a), ToImVec2(*b));
}

IGEXPORT void axPinPivotSize(axVec2* size)
{
    ed::PinPivotSize(ToImVec2(*size));
}

IGEXPORT void axPinPivotScale(axVec2* scale)
{
    ed::PinPivotScale(ToImVec2(*scale));
}

IGEXPORT void axPinPivotAlignment(axVec2* alignment)
{
    ed::PinPivotAlignment(ToImVec2(*alignment));
}

IGEXPORT void axEndPin()
{
    ed::EndPin();
}

IGEXPORT void axGroup(axVec2* size)
{
    ed::Group(ToImVec2(*size));
}

IGEXPORT void axEndNode()
{
    ed::EndNode();
}

IGEXPORT int axBeginGroupHint(axNodeId nodeId)
{
    return ed::BeginGroupHint(NodeFromPtr(nodeId)) ? 1 : 0;
}

IGEXPORT void axGetGroupMin(axVec2 *gmin)
{
    *gmin = FromImVec2(ed::GetGroupMin());
}

IGEXPORT void axGetGroupMax(axVec2 *gmax)
{
    *gmax = FromImVec2(ed::GetGroupMax());
}

IGEXPORT void* axGetHintForegroundDrawList()
{
    return (void*)ed::GetHintForegroundDrawList();
}

IGEXPORT void* axGetHintBackgroundDrawList()
{
    return (void*)ed::GetHintBackgroundDrawList();
}

IGEXPORT void axEndGroupHint()
{
    ed::EndGroupHint();
}

IGEXPORT void* axGetNodeBackgroundDrawList(axNodeId nodeId)
{
    return (void*)ed::GetNodeBackgroundDrawList(NodeFromPtr(nodeId));
}

IGEXPORT int axLink(axLinkId id, axPinId startPinId, axPinId endPinId, axVec4* color, float thickness)
{
    return ed::Link(LinkFromPtr(id), PinFromPtr(startPinId), PinFromPtr(endPinId), ToImVec4(*color), thickness) ? 1 : 0;
}


IGEXPORT void axFlow(axLinkId linkId, axFlowDirection direction)
{
    ed::Flow(LinkFromPtr(linkId), (ed::FlowDirection)direction);
}

IGEXPORT int axBeginCreate(axVec4* color, float thickness)
{
    return ed::BeginCreate(ToImVec4(*color), thickness);
}

IGEXPORT int axQueryNewLink(axPinId* startId, axPinId* endId)
{
    ed::PinId sp{};
    ed::PinId ep{};
    int retval = ed::QueryNewLink(&sp, &ep) ? 1 : 0;
    if(startId) *startId = PtrFromPin(sp);
    if(endId) *endId = PtrFromPin(ep);
    return retval;
}

IGEXPORT int axQueryNewLink_Styled(axPinId* startId, axPinId* endId, axVec4* color, float thickness)
{
    ed::PinId sp{};
    ed::PinId ep{};
    int retval = ed::QueryNewLink(&sp, &ep, ToImVec4(*color), thickness) ? 1 : 0;
    if(startId) *startId = PtrFromPin(sp);
    if(endId) *endId = PtrFromPin(ep);
    return retval;
}

IGEXPORT int axQueryNewNode(axPinId* pinId)
{
    ed::PinId p{};
    int retval = ed::QueryNewNode(&p);
    if(pinId) *pinId = PtrFromPin(p);
    return retval;
}

IGEXPORT int axQueryNewNode_Styled(axPinId* pinId, axVec4* color, float thickness)
{
    ed::PinId p{};
    int retval = ed::QueryNewNode(&p, ToImVec4(*color), thickness);
    if (pinId) *pinId = PtrFromPin(p);
    return retval;
}

IGEXPORT int axAcceptNewItem()
{
    return ed::AcceptNewItem() ? 1 : 0;
}
IGEXPORT int axAcceptNewItem_Styled(axVec4* color, float thickness)
{
    return ed::AcceptNewItem(ToImVec4(*color), thickness) ? 1 : 0;
}

IGEXPORT void axRejectNewItem()
{
    ed::RejectNewItem();
}

IGEXPORT void axRejectNewItem_Styled(axVec4* color, float thickness)
{
    ed::RejectNewItem(ToImVec4(*color), thickness);
}

IGEXPORT void axEndCreate()
{
    ed::EndCreate();
}

IGEXPORT int axBeginDelete()
{
    return ed::BeginDelete() ? 1 : 0;
}

IGEXPORT int axQueryDeletedLink(axLinkId* linkId, axPinId* startId, axPinId* endId)
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

IGEXPORT int axQueryDeletedNode(axNodeId* nodeId)
{
    ed::NodeId nd{};
    int retval = ed::QueryDeletedNode(&nd) ? 1 : 0;
    if(nodeId) *nodeId = PtrFromNode(nd);
    return retval;
}
IGEXPORT int axAcceptDeletedItem(int deleteDependencies)
{
    return ed::AcceptDeletedItem(deleteDependencies != 0) ? 1 : 0;
}
IGEXPORT void axRejectDeletedItem()
{
    ed::RejectDeletedItem();
}
IGEXPORT void axEndDelete()
{
    ed::EndDelete();
}

IGEXPORT void axSetNodePosition(axNodeId nodeId, axVec2* editorPosition)
{
    ed::SetNodePosition(NodeFromPtr(nodeId), ToImVec2(*editorPosition));
}

IGEXPORT void axSetGroupSize(axNodeId nodeId, axVec2* size)
{
    ed::SetGroupSize(NodeFromPtr(nodeId), ToImVec2(*size));
}
IGEXPORT void axGetNodePosition(axNodeId nodeId, axVec2* pos)
{
    *pos = FromImVec2(ed::GetNodePosition(NodeFromPtr(nodeId)));
}
IGEXPORT void axGetNodeSize(axNodeId nodeId, axVec2* sz)
{
    *sz = FromImVec2(ed::GetNodeSize(NodeFromPtr(nodeId)));
}
IGEXPORT void axCenterNodeOnScreen(axNodeId nodeId)
{
    ed::CenterNodeOnScreen(NodeFromPtr(nodeId));
}
IGEXPORT void axSetNodeZPosition(axNodeId nodeId, float z)
{
    ed::SetNodeZPosition(NodeFromPtr(nodeId), z);
}

IGEXPORT float axGetNodeZPosition(axNodeId nodeId)
{
    return ed::GetNodeZPosition(NodeFromPtr(nodeId));
}
IGEXPORT void axSuspend()
{
    ed::Suspend();
}

IGEXPORT void axResume()
{
    ed::Resume();
}

IGEXPORT int axIsSuspended()
{
    return ed::IsSuspended() ? 1 : 0;
}

IGEXPORT int axIsActive()
{
    return ed::IsActive() ? 1 : 0;
}

IGEXPORT int axHasSelectionChanged()
{
    return ed::HasSelectionChanged() ? 1 : 0;
}
IGEXPORT int axGetSelectedObjectCount()
{
    return ed::GetSelectedObjectCount();
}

IGEXPORT int axGetSelectedNodes(axNodeId* nodes, int size)
{
    ed::NodeId* ids = (ed::NodeId*)ig_alloca(sizeof(ed::NodeId) * size);
    int retval = ed::GetSelectedNodes(ids, size);
    for(int i = 0; i < size && i < retval; i++)
        nodes[i] = PtrFromNode(ids[i]);
    return retval;
}
IGEXPORT int axGetSelectedLinks(axLinkId* links, int size)
{
    ed::LinkId* ids = (ed::LinkId*)ig_alloca(sizeof(ed::LinkId) * size);
    int retval = ed::GetSelectedLinks(ids, size);
    for(int i = 0; i < size && i < retval; i++)
        links[i] = PtrFromLink(ids[i]);
    return retval;
}
IGEXPORT int axIsNodeSelected(axNodeId nodeId)
{
    return ed::IsNodeSelected(NodeFromPtr(nodeId)) ? 1 : 0;
}
IGEXPORT int axIsLinkSelected(axLinkId linkId)
{
    return ed::IsLinkSelected(LinkFromPtr(linkId)) ? 1 : 0;
}
IGEXPORT void axClearSelection()
{
    ed::ClearSelection();
}
IGEXPORT void axSelectNode(axNodeId nodeId, int append)
{
    ed::SelectNode(NodeFromPtr(nodeId), append != 0);
}
IGEXPORT void axSelectLink(axLinkId linkId, int append)
{
    ed::SelectLink(LinkFromPtr(linkId), append != 0);
}
IGEXPORT void axDeselectNode(axNodeId nodeId)
{
    ed::DeselectNode(NodeFromPtr(nodeId));
}

IGEXPORT void axDeselectLink(axLinkId linkId)
{
    ed::DeselectLink(LinkFromPtr(linkId));
}

IGEXPORT int axDeleteNode(axNodeId nodeId)
{
    return ed::DeleteNode(NodeFromPtr(nodeId)) ? 1 : 0;
}

IGEXPORT int axDeleteLink(axLinkId linkId)
{
    return ed::DeleteLink(LinkFromPtr(linkId)) ? 1 : 0;
}



IGEXPORT int axNodeHasAnyLinks(axNodeId nodeId) // Returns true if node has any link connected
{
    return ed::HasAnyLinks(NodeFromPtr(nodeId)) ? 1 : 0;
}
IGEXPORT int axPinHasAnyLinks(axPinId pinId) // Return true if pin has any link connected
{
    return ed::HasAnyLinks(PinFromPtr(pinId)) ? 1 : 0;
}
IGEXPORT int axNodeBreakLinks(axNodeId nodeId) // Break all links connected to this node
{
    return ed::BreakLinks(NodeFromPtr(nodeId));
}
IGEXPORT int axPinBreakLinks(axPinId pinId) // Break all links connected to this pin
{
    return ed::BreakLinks(PinFromPtr(pinId));
}

IGEXPORT void axNavigateToContent(float duration)
{
    ed::NavigateToContent(duration);
}
IGEXPORT void axNavigateToSelection(int zoomIn, float duration)
{
    ed::NavigateToSelection(zoomIn != 0, duration);
}

IGEXPORT void axEnableShortcuts(int enable)
{
    ed::EnableShortcuts(enable != 0);
}

IGEXPORT int axAreShortcutsEnabled()
{
    return ed::AreShortcutsEnabled() ? 1 : 0;
}

IGEXPORT int axShowNodeContextMenu(axNodeId* nodeId)
{
    ed::NodeId n{};
    bool rv = ed::ShowNodeContextMenu(&n);
    *nodeId = PtrFromNode(n);
    return rv ? 1 : 0;
}

IGEXPORT int axShowPinContextMenu(axPinId* pinId)
{
    ed::PinId p{};
    bool rv = ed::ShowPinContextMenu(&p);
    *pinId = PtrFromPin(p);
    return rv ? 1 : 0;
}

IGEXPORT int axShowLinkContextMenu(axLinkId* linkId)
{
    ed::LinkId l{};
    bool rv = ed::ShowLinkContextMenu(&l);
    *linkId = PtrFromLink(l);
    return rv ? 1 : 0;
}

IGEXPORT int axShowBackgroundContextMenu()
{
    return ed::ShowBackgroundContextMenu() ? 1 : 0;
}

IGEXPORT int axBeginShortcut()
{
    return ed::BeginShortcut() ? 1 : 0;
}
IGEXPORT int axAcceptCut()
{
    return ed::AcceptCut() ? 1 : 0;
}
IGEXPORT int axAcceptCopy()
{
    return ed::AcceptCopy() ? 1 : 0;
}
IGEXPORT int axAcceptPaste()
{
    return ed::AcceptPaste() ? 1 : 0;
}
IGEXPORT int axAcceptDuplicate()
{
    return ed::AcceptDuplicate() ? 1 : 0;
}
IGEXPORT int axAcceptCreateNode()
{
    return ed::AcceptCreateNode() ? 1 : 0;
}
IGEXPORT int axGetActionContextSize()
{
    return ed::GetActionContextSize();
}
IGEXPORT int axGetActionContextNodes(axNodeId* nodes, int size)
{
    ed::NodeId* ids = (ed::NodeId*)ig_alloca(sizeof(ed::NodeId) * size);
    int retval = ed::GetActionContextNodes(ids, size);
    for(int i = 0; i < size && i < retval; i++)
        nodes[i] = PtrFromNode(ids[i]);
    return retval;
}

IGEXPORT int axGetActionContextLinks(axLinkId* links, int size)
{
    ed::LinkId* ids = (ed::LinkId*)ig_alloca(sizeof(ed::LinkId) * size);
    int retval = ed::GetActionContextLinks(ids, size);
    for(int i = 0; i < size && i < retval; i++)
        links[i] = PtrFromLink(ids[i]);
    return retval;
}

IGEXPORT void axEndShortcut()
{
    ed::EndShortcut();
}

IGEXPORT float axGetCurrentZoom()
{
    return ed::GetCurrentZoom();
}

IGEXPORT axNodeId axGetHoveredNode()
{
    return PtrFromNode(ed::GetHoveredNode());
}

IGEXPORT axPinId axGetHoveredPin()
{
    return PtrFromPin(ed::GetHoveredPin());
}

IGEXPORT axLinkId axGetHoveredLink()
{
    return PtrFromLink(ed::GetHoveredLink());
}

IGEXPORT axNodeId axGetDoubleClickedNode()
{
    return PtrFromNode(ed::GetDoubleClickedNode());
}

IGEXPORT axPinId axGetDoubleClickedPin()
{
    return PtrFromPin(ed::GetDoubleClickedPin());
}

IGEXPORT axLinkId axGetDoubleClickedLink()
{
    return PtrFromLink(ed::GetDoubleClickedLink());
}

IGEXPORT int axIsBackgroundClicked()
{
    return ed::IsBackgroundClicked() ? 1 : 0;
}

IGEXPORT int axIsBackgroundDoubleClicked()
{
    return ed::IsBackgroundDoubleClicked() ? 1 : 0;
}

IGEXPORT int axGetBackgroundClickButtonIndex() // -1 if none
{
    return (int)ed::GetBackgroundClickButtonIndex();
}

IGEXPORT int axGetBackgroundDoubleClickButtonIndex()
{
    return (int)ed::GetBackgroundDoubleClickButtonIndex();
}

IGEXPORT int axGetLinkPins(axLinkId linkId, axPinId* startPinId, axPinId* endPinId)
{
    ed::PinId sp {};
    ed::PinId ep {};
    int retval = ed::GetLinkPins(LinkFromPtr(linkId), startPinId ? &sp : nullptr, endPinId ? &ep : nullptr) ? 1 : 0;
    if(startPinId) *startPinId = PtrFromPin(sp);
    if(endPinId) *endPinId = PtrFromPin(ep);
    return retval;
}

IGEXPORT int axPinHadAnyLinks(axPinId pinId)
{
    return ed::PinHadAnyLinks(PinFromPtr(pinId)) ? 1 : 0;
}


IGEXPORT void axGetScreenSize(axVec2* size)
{
    *size = FromImVec2(ed::GetScreenSize());
}

IGEXPORT void axScreenToCanvas(axVec2* pos, axVec2* canvas)
{
    *canvas = FromImVec2(ed::ScreenToCanvas(ToImVec2(*pos)));
}

IGEXPORT void axCanvasToScreen(axVec2* pos, axVec2* screen)
{
    *screen = FromImVec2(ed::CanvasToScreen(ToImVec2(*pos)));
}

IGEXPORT int axGetNodeCount()
{
    return ed::GetNodeCount();
}

IGEXPORT int axGetOrderedNodeIds(axNodeId* nodes, int size)
{
    ed::NodeId* ids = (ed::NodeId*)ig_alloca(sizeof(ed::NodeId) * size);
    int retval = ed::GetOrderedNodeIds(ids, size);
    for(int i = 0; i < size && i < retval; i++)
        nodes[i] = PtrFromNode(ids[i]);
    return retval;
}
