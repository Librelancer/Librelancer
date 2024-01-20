namespace LibreLancer.ImUI.NodeEditor;

using System;
using System.Numerics;
using System.Runtime.InteropServices;

using axNodeId = System.IntPtr;
using axPinId = System.IntPtr;
using axLinkId = System.IntPtr;
using axConfigSession = System.IntPtr;
using axConfigSaveSettings = System.IntPtr;
using axConfigLoadSettings = System.IntPtr;
using axConfigSaveNodeSettings = System.IntPtr;
using axConfigLoadNodeSettings = System.IntPtr;

static unsafe class NodeEditorNative
{
    [DllImport("cimgui")]
    public static extern IntPtr axConfigNew();

    [DllImport("cimgui")]
    public static extern void axConfigFree(IntPtr config);

    [DllImport("cimgui")]
    public static extern IntPtr axConfig_get_SettingsFile(IntPtr config);

    [DllImport("cimgui")]
    public static extern void axConfig_set_SettingsFile(IntPtr config, IntPtr settingsFile);

    [DllImport("cimgui")]
    public static extern axConfigSession axConfig_get_BeginSaveSession(IntPtr config);

    [DllImport("cimgui")]
    public static extern void axConfig_set_BeginSaveSession(IntPtr config, axConfigSession beginSaveSession);

    [DllImport("cimgui")]
    public static extern axConfigSession axConfig_get_EndSaveSession(IntPtr config);

    [DllImport("cimgui")]
    public static extern void axConfig_set_EndSaveSession(IntPtr config, axConfigSession endSaveSession);

    [DllImport("cimgui")]
    public static extern axConfigSaveSettings axConfig_get_SaveSettings(IntPtr config);

    [DllImport("cimgui")]
    public static extern void axConfig_set_SaveSettings(IntPtr config, axConfigSaveSettings saveSettings);

    [DllImport("cimgui")]
    public static extern axConfigLoadSettings axConfig_get_LoadSettings(IntPtr config);

    [DllImport("cimgui")]
    public static extern void axConfig_set_LoadSettings(IntPtr config, axConfigLoadSettings loadSettings);

    [DllImport("cimgui")]
    public static extern axConfigSaveNodeSettings axConfig_get_SaveNodeSettings(IntPtr config);

    [DllImport("cimgui")]
    public static extern void axConfig_set_SaveNodeSettings(IntPtr config, axConfigSaveNodeSettings saveNodeSettings);

    [DllImport("cimgui")]
    public static extern axConfigLoadNodeSettings axConfig_get_LoadNodeSettings(IntPtr config);

    [DllImport("cimgui")]
    public static extern void axConfig_set_LoadNodeSettings(IntPtr config, axConfigLoadNodeSettings loadNodeSettings);


    [DllImport("cimgui")]
    public static extern void* axConfig_get_UserPointer(IntPtr config);

    [DllImport("cimgui")]
    public static extern void axConfig_set_UserPointer(IntPtr config, void* userPointer);

//TODO: Custom Zoom Levels

    [DllImport("cimgui")]
    public static extern CanvasSizeMode axConfig_get_CanvasSizeMode(IntPtr config);

    [DllImport("cimgui")]
    public static extern void axConfig_set_CanvasSizeMode(IntPtr config, CanvasSizeMode canvasSizeMode);

    [DllImport("cimgui")]
    public static extern int axConfig_get_DragButtonIndex(IntPtr config);

    [DllImport("cimgui")]
    public static extern void axConfig_set_DragButtonIndex(IntPtr config, int dragButtonIndex);

    [DllImport("cimgui")]
    public static extern int axConfig_get_SelectButtonIndex(IntPtr config);

    [DllImport("cimgui")]
    public static extern void axConfig_set_SelectButtonIndex(IntPtr config, int selectButtonIndex);

    [DllImport("cimgui")]
    public static extern int axConfig_get_NavigateButtonIndex(IntPtr config);

    [DllImport("cimgui")]
    public static extern void axConfig_set_NavigateButtonIndex(IntPtr config, int navigateButtonIndex);

    [DllImport("cimgui")]
    public static extern int axConfig_get_ContextMenuButtonIndex(IntPtr config);

    [DllImport("cimgui")]
    public static extern void axConfig_set_ContextMenuButtonIndex(IntPtr config, int contextMenuButtonIndex);


    [DllImport("cimgui")]
    public static extern void axSetCurrentEditor(IntPtr ctx);

    [DllImport("cimgui")]
    public static extern IntPtr axGetCurrentEditor();

    [DllImport("cimgui")]
    public static extern IntPtr axCreateEditor(IntPtr config);

    [DllImport("cimgui")]
    public static extern void axDestroyEditor(IntPtr ctx);

    [DllImport("cimgui")]
    public static extern IntPtr axGetConfig(IntPtr ctx);

    [DllImport("cimgui")]
    public static extern Style* axGetStyle();

    [DllImport("cimgui")]
    public static extern IntPtr axGetStyleColorName(StyleColor colorIndex);

    [DllImport("cimgui")]
    public static extern void axPushStyleColor(StyleColor colorIndex, Vector4* color);

    [DllImport("cimgui")]
    public static extern void axPopStyleColor(int count);

    [DllImport("cimgui")]
    public static extern void axPushStyleVar1(StyleVar varIndex, float value);

    [DllImport("cimgui")]
    public static extern void axPushStyleVar2(StyleVar varIndex, Vector2* value);

    [DllImport("cimgui")]
    public static extern void axPushStyleVar4(StyleVar varIndex, Vector4* value);

    [DllImport("cimgui")]
    public static extern void axPopStyleVar(int count);

    [DllImport("cimgui")]
    public static extern void axBegin(IntPtr id, Vector2* size);

    [DllImport("cimgui")]
    public static extern void axEnd();

    [DllImport("cimgui")]
    public static extern void axBeginNode(axNodeId id);

    [DllImport("cimgui")]
    public static extern void axBeginPin(axPinId id, PinKind kind);

    [DllImport("cimgui")]
    public static extern void axPinRect(Vector2* a, Vector2* b);

    [DllImport("cimgui")]
    public static extern void axPinPivotRect(Vector2* a, Vector2* b);

    [DllImport("cimgui")]
    public static extern void axPinPivotSize(Vector2* size);

    [DllImport("cimgui")]
    public static extern void axPinPivotScale(Vector2* scale);

    [DllImport("cimgui")]
    public static extern void axPinPivotAlignment(Vector2* alignment);

    [DllImport("cimgui")]
    public static extern void axEndPin();

    [DllImport("cimgui")]
    public static extern void axGroup(Vector2* size);

    [DllImport("cimgui")]
    public static extern void axEndNode();

    [DllImport("cimgui")]
    public static extern int axBeginGroupHint(axNodeId nodeId);

    [DllImport("cimgui")]
    public static extern void axGetGroupMin(Vector2* gmin);

    [DllImport("cimgui")]
    public static extern void axGetGroupMax(Vector2* gmax);

    [DllImport("cimgui")]
    public static extern void* axGetHintForegroundDrawList();

    [DllImport("cimgui")]
    public static extern void* axGetHintBackgroundDrawList();

    [DllImport("cimgui")]
    public static extern void axEndGroupHint();

    [DllImport("cimgui")]
    public static extern void* axGetNodeBackgroundDrawList(axNodeId nodeId);

    [DllImport("cimgui")]
    public static extern int axLink(axLinkId id, axPinId startPinId, axPinId endPinId, Vector4* color, float thickness);

    [DllImport("cimgui")]
    public static extern void axFlow(axLinkId linkId, FlowDirection direction);

    [DllImport("cimgui")]
    public static extern int axBeginCreate(Vector4* color, float thickness);

    [DllImport("cimgui")]
    public static extern int axQueryNewLink(axPinId* startId, axPinId* endId);

    [DllImport("cimgui")]
    public static extern int axQueryNewLink_Styled(axPinId* startId, axPinId* endId, Vector4* color, float thickness);

    [DllImport("cimgui")]
    public static extern int axQueryNewNode(axPinId* pinId);

    [DllImport("cimgui")]
    public static extern int axQueryNewNode_Styled(axPinId* pinId, Vector4* color, float thickness);

    [DllImport("cimgui")]
    public static extern int axAcceptNewItem();

    [DllImport("cimgui")]
    public static extern int axAcceptNewItem_Styled(Vector4* color, float thickness);

    [DllImport("cimgui")]
    public static extern void axRejectNewItem();

    [DllImport("cimgui")]
    public static extern void axRejectNewItem_Styled(Vector4* color, float thickness);

    [DllImport("cimgui")]
    public static extern void axEndCreate();

    [DllImport("cimgui")]
    public static extern int axBeginDelete();

    [DllImport("cimgui")]
    public static extern int axQueryDeletedLink(axLinkId* linkId, axPinId* startId, axPinId* endId);

    [DllImport("cimgui")]
    public static extern int axQueryDeletedNode(axNodeId* nodeId);

    [DllImport("cimgui")]
    public static extern int axAcceptDeletedItem(int deleteDependencies);

    [DllImport("cimgui")]
    public static extern void axRejectDeletedItem();

    [DllImport("cimgui")]
    public static extern void axEndDelete();

    [DllImport("cimgui")]
    public static extern void axSetNodePosition(axNodeId nodeId, Vector2* editorPosition);

    [DllImport("cimgui")]
    public static extern void axSetGroupSize(axNodeId nodeId, Vector2* size);

    [DllImport("cimgui")]
    public static extern void axGetNodePosition(axNodeId nodeId, Vector2* pos);

    [DllImport("cimgui")]
    public static extern void axGetNodeSize(axNodeId nodeId, Vector2* sz);

    [DllImport("cimgui")]
    public static extern void axCenterNodeOnScreen(axNodeId nodeId);

    [DllImport("cimgui")]
    public static extern void
        axSetNodeZPosition(axNodeId nodeId,
            float z); // Sets node z position, nodes with higher value are drawn over nodes with lower value

    [DllImport("cimgui")]
    public static extern float axGetNodeZPosition(axNodeId nodeId); // Returns node z position, defaults is 0.0f

    [DllImport("cimgui")]
    public static extern void axRestoreNodeState(axNodeId nodeId);

    [DllImport("cimgui")]
    public static extern void axSuspend();

    [DllImport("cimgui")]
    public static extern void axResume();

    [DllImport("cimgui")]
    public static extern int axIsSuspended();

    [DllImport("cimgui")]
    public static extern int axIsActive();

    [DllImport("cimgui")]
    public static extern int axHasSelectionChanged();

    [DllImport("cimgui")]
    public static extern int axGetSelectedObjectCount();

    [DllImport("cimgui")]
    public static extern int axGetSelectedNodes(axNodeId* nodes, int size);

    [DllImport("cimgui")]
    public static extern int axGetSelectedLinks(axLinkId* links, int size);

    [DllImport("cimgui")]
    public static extern int axIsNodeSelected(axNodeId nodeId);

    [DllImport("cimgui")]
    public static extern int axIsLinkSelected(axLinkId linkId);

    [DllImport("cimgui")]
    public static extern void axClearSelection();

    [DllImport("cimgui")]
    public static extern void axSelectNode(axNodeId nodeId, int append);

    [DllImport("cimgui")]
    public static extern void axSelectLink(axLinkId linkId, int append);

    [DllImport("cimgui")]
    public static extern void axDeselectNode(axNodeId nodeId);

    [DllImport("cimgui")]
    public static extern void axDeselectLink(axLinkId linkId);

    [DllImport("cimgui")]
    public static extern int axDeleteNode(axNodeId nodeId);

    [DllImport("cimgui")]
    public static extern int axDeleteLink(axLinkId linkId);

    [DllImport("cimgui")]
    public static extern int axNodeHasAnyLinks(axNodeId nodeId); // Returns true if node has any link connected

    [DllImport("cimgui")]
    public static extern int axPinHasAnyLinks(axPinId pinId); // Return true if pin has any link connected

    [DllImport("cimgui")]
    public static extern int axNodeBreakLinks(axNodeId nodeId); // Break all links connected to this node

    [DllImport("cimgui")]
    public static extern int axPinBreakLinks(axPinId pinId); // Break all links connected to this pin

    [DllImport("cimgui")]
    public static extern void axNavigateToContent(float duration);

    [DllImport("cimgui")]
    public static extern void axNavigateToSelection(int zoomIn, float duration);

    [DllImport("cimgui")]
    public static extern int axShowNodeContextMenu(axNodeId* nodeId);

    [DllImport("cimgui")]
    public static extern int axShowPinContextMenu(axPinId* pinId);

    [DllImport("cimgui")]
    public static extern int axShowLinkContextMenu(axLinkId* linkId);

    [DllImport("cimgui")]
    public static extern int axShowBackgroundContextMenu();

    [DllImport("cimgui")]
    public static extern void axEnableShortcuts(int enable);

    [DllImport("cimgui")]
    public static extern int axAreShortcutsEnabled();

    [DllImport("cimgui")]
    public static extern int axBeginShortcut();

    [DllImport("cimgui")]
    public static extern int axAcceptCut();

    [DllImport("cimgui")]
    public static extern int axAcceptCopy();

    [DllImport("cimgui")]
    public static extern int axAcceptPaste();

    [DllImport("cimgui")]
    public static extern int axAcceptDuplicate();

    [DllImport("cimgui")]
    public static extern int axAcceptCreateNode();

    [DllImport("cimgui")]
    public static extern int axGetActionContextSize();

    [DllImport("cimgui")]
    public static extern int axGetActionContextNodes(axNodeId* nodes, int size);

    [DllImport("cimgui")]
    public static extern int axGetActionContextLinks(axLinkId* links, int size);

    [DllImport("cimgui")]
    public static extern void axEndShortcut();

    [DllImport("cimgui")]
    public static extern float axGetCurrentZoom();

    [DllImport("cimgui")]
    public static extern axNodeId axGetHoveredNode();

    [DllImport("cimgui")]
    public static extern axPinId axGetHoveredPin();

    [DllImport("cimgui")]
    public static extern axLinkId axGetHoveredLink();

    [DllImport("cimgui")]
    public static extern axNodeId axGetDoubleClickedNode();

    [DllImport("cimgui")]
    public static extern axPinId axGetDoubleClickedPin();

    [DllImport("cimgui")]
    public static extern axLinkId axGetDoubleClickedLink();

    [DllImport("cimgui")]
    public static extern int axIsBackgroundClicked();

    [DllImport("cimgui")]
    public static extern int axIsBackgroundDoubleClicked();

    [DllImport("cimgui")]
    public static extern int axGetBackgroundClickButtonIndex(); // -1 if none

    [DllImport("cimgui")]
    public static extern int axGetBackgroundDoubleClickButtonIndex(); // -1 if none

    [DllImport("cimgui")]
    public static extern int
        axGetLinkPins(axLinkId linkId, axPinId* startPinId,
            axPinId* endPinId); // pass nullptr if particular pin do not interest you

    [DllImport("cimgui")]
    public static extern int axPinHadAnyLinks(axPinId pinId);

    [DllImport("cimgui")]
    public static extern void axGetScreenSize(Vector2* size);

    [DllImport("cimgui")]
    public static extern void axScreenToCanvas(Vector2* pos, Vector2* canvas);

    [DllImport("cimgui")]
    public static extern void axCanvasToScreen(Vector2* pos, Vector2* screen);

    [DllImport("cimgui")]
    public static extern int axGetNodeCount();

    [DllImport("cimgui")]
    public static extern int axGetOrderedNodeIds(axNodeId* nodes, int size);
}
