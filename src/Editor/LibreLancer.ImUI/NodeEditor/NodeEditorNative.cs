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
using axConfigNodeDraggedHook = System.IntPtr;
using axConfigNodeResizedHook = System.IntPtr;

static unsafe partial class NodeEditorNative
{
    [LibraryImport("cimgui")]
    public static partial IntPtr axConfigNew();

    [LibraryImport("cimgui")]
    public static partial void axConfigFree(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial IntPtr axConfig_get_SettingsFile(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_SettingsFile(IntPtr config, IntPtr settingsFile);

    [LibraryImport("cimgui")]
    public static partial axConfigSession axConfig_get_BeginSaveSession(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_BeginSaveSession(IntPtr config, axConfigSession beginSaveSession);

    [LibraryImport("cimgui")]
    public static partial axConfigSession axConfig_get_EndSaveSession(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_EndSaveSession(IntPtr config, axConfigSession endSaveSession);

    [LibraryImport("cimgui")]
    public static partial axConfigSaveSettings axConfig_get_SaveSettings(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_SaveSettings(IntPtr config, axConfigSaveSettings saveSettings);

    [LibraryImport("cimgui")]
    public static partial axConfigLoadSettings axConfig_get_LoadSettings(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_LoadSettings(IntPtr config, axConfigLoadSettings loadSettings);

    [LibraryImport("cimgui")]
    public static partial axConfigSaveNodeSettings axConfig_get_SaveNodeSettings(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_SaveNodeSettings(IntPtr config, axConfigSaveNodeSettings saveNodeSettings);

    [LibraryImport("cimgui")]
    public static partial axConfigLoadNodeSettings axConfig_get_LoadNodeSettings(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_LoadNodeSettings(IntPtr config, axConfigLoadNodeSettings loadNodeSettings);


    [LibraryImport("cimgui")]
    public static partial axConfigNodeDraggedHook axConfig_get_NodeDraggedHook(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_NodeDraggedHook(IntPtr config, axConfigNodeDraggedHook loadNodeSettings);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_NodeResizedHook(IntPtr config, axConfigNodeResizedHook loadNodeSettings);

    [LibraryImport("cimgui")]
    public static partial void* axConfig_get_UserPointer(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_UserPointer(IntPtr config, void* userPointer);

    [LibraryImport("cimgui")]
    public static partial int axConfig_get_EnableSmoothZoom(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_EnableSmoothZoom(IntPtr config, int enableSmoothZoom);

    [LibraryImport("cimgui")]
    public static partial float axConfig_get_SmoothZoomPower(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_SmoothZoomPower(IntPtr config, float smoothZoomPower);

//TODO: Custom Zoom Levels

    [LibraryImport("cimgui")]
    public static partial CanvasSizeMode axConfig_get_CanvasSizeMode(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_CanvasSizeMode(IntPtr config, CanvasSizeMode canvasSizeMode);

    [LibraryImport("cimgui")]
    public static partial int axConfig_get_DragButtonIndex(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_DragButtonIndex(IntPtr config, int dragButtonIndex);

    [LibraryImport("cimgui")]
    public static partial int axConfig_get_SelectButtonIndex(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_SelectButtonIndex(IntPtr config, int selectButtonIndex);

    [LibraryImport("cimgui")]
    public static partial int axConfig_get_NavigateButtonIndex(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_NavigateButtonIndex(IntPtr config, int navigateButtonIndex);

    [LibraryImport("cimgui")]
    public static partial int axConfig_get_ContextMenuButtonIndex(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axConfig_set_ContextMenuButtonIndex(IntPtr config, int contextMenuButtonIndex);


    [LibraryImport("cimgui")]
    public static partial void axSetCurrentEditor(IntPtr ctx);

    [LibraryImport("cimgui")]
    public static partial IntPtr axGetCurrentEditor();

    [LibraryImport("cimgui")]
    public static partial IntPtr axCreateEditor(IntPtr config);

    [LibraryImport("cimgui")]
    public static partial void axDestroyEditor(IntPtr ctx);

    [LibraryImport("cimgui")]
    public static partial IntPtr axGetConfig(IntPtr ctx);

    [LibraryImport("cimgui")]
    public static partial Style* axGetStyle();

    [LibraryImport("cimgui")]
    public static partial IntPtr axGetStyleColorName(StyleColor colorIndex);

    [LibraryImport("cimgui")]
    public static partial void axPushStyleColor(StyleColor colorIndex, Vector4* color);

    [LibraryImport("cimgui")]
    public static partial void axPopStyleColor(int count);

    [LibraryImport("cimgui")]
    public static partial void axPushStyleVar1(StyleVar varIndex, float value);

    [LibraryImport("cimgui")]
    public static partial void axPushStyleVar2(StyleVar varIndex, Vector2* value);

    [LibraryImport("cimgui")]
    public static partial void axPushStyleVar4(StyleVar varIndex, Vector4* value);

    [LibraryImport("cimgui")]
    public static partial void axPopStyleVar(int count);

    [LibraryImport("cimgui")]
    public static partial void axBegin(IntPtr id, Vector2* size);

    [LibraryImport("cimgui")]
    public static partial void axEnd();

    [LibraryImport("cimgui")]
    public static partial void axBeginNode(axNodeId id);

    [LibraryImport("cimgui")]
    public static partial void axBeginPin(axPinId id, PinKind kind);

    [LibraryImport("cimgui")]
    public static partial void axPinRect(Vector2* a, Vector2* b);

    [LibraryImport("cimgui")]
    public static partial void axPinPivotRect(Vector2* a, Vector2* b);

    [LibraryImport("cimgui")]
    public static partial void axPinPivotSize(Vector2* size);

    [LibraryImport("cimgui")]
    public static partial void axPinPivotScale(Vector2* scale);

    [LibraryImport("cimgui")]
    public static partial void axPinPivotAlignment(Vector2* alignment);

    [LibraryImport("cimgui")]
    public static partial void axEndPin();

    [LibraryImport("cimgui")]
    public static partial void axGroup(Vector2* size);

    [LibraryImport("cimgui")]
    public static partial void axEndNode();

    [LibraryImport("cimgui")]
    public static partial int axBeginGroupHint(axNodeId nodeId);

    [LibraryImport("cimgui")]
    public static partial void axGetGroupMin(Vector2* gmin);

    [LibraryImport("cimgui")]
    public static partial void axGetGroupMax(Vector2* gmax);

    [LibraryImport("cimgui")]
    public static partial void* axGetHintForegroundDrawList();

    [LibraryImport("cimgui")]
    public static partial void* axGetHintBackgroundDrawList();

    [LibraryImport("cimgui")]
    public static partial void axEndGroupHint();

    [LibraryImport("cimgui")]
    public static partial void* axGetNodeBackgroundDrawList(axNodeId nodeId);

    [LibraryImport("cimgui")]
    public static partial int axLink(axLinkId id, axPinId startPinId, axPinId endPinId, Vector4* color, float thickness);

    [LibraryImport("cimgui")]
    public static partial void axFlow(axLinkId linkId, FlowDirection direction);

    [LibraryImport("cimgui")]
    public static partial int axBeginCreate(Vector4* color, float thickness);

    [LibraryImport("cimgui")]
    public static partial int axQueryNewLink(axPinId* startId, axPinId* endId);

    [LibraryImport("cimgui")]
    public static partial int axQueryNewLink_Styled(axPinId* startId, axPinId* endId, Vector4* color, float thickness);

    [LibraryImport("cimgui")]
    public static partial int axQueryNewNode(axPinId* pinId);

    [LibraryImport("cimgui")]
    public static partial int axQueryNewNode_Styled(axPinId* pinId, Vector4* color, float thickness);

    [LibraryImport("cimgui")]
    public static partial int axAcceptNewItem();

    [LibraryImport("cimgui")]
    public static partial int axAcceptNewItem_Styled(Vector4* color, float thickness);

    [LibraryImport("cimgui")]
    public static partial void axRejectNewItem();

    [LibraryImport("cimgui")]
    public static partial void axRejectNewItem_Styled(Vector4* color, float thickness);

    [LibraryImport("cimgui")]
    public static partial void axEndCreate();

    [LibraryImport("cimgui")]
    public static partial int axBeginDelete();

    [LibraryImport("cimgui")]
    public static partial int axQueryDeletedLink(axLinkId* linkId, axPinId* startId, axPinId* endId);

    [LibraryImport("cimgui")]
    public static partial int axQueryDeletedNode(axNodeId* nodeId);

    [LibraryImport("cimgui")]
    public static partial int axAcceptDeletedItem(int deleteDependencies);

    [LibraryImport("cimgui")]
    public static partial void axRejectDeletedItem();

    [LibraryImport("cimgui")]
    public static partial void axEndDelete();

    [LibraryImport("cimgui")]
    public static partial void axSetNodePosition(axNodeId nodeId, Vector2* editorPosition);

    [LibraryImport("cimgui")]
    public static partial void axSetGroupSize(axNodeId nodeId, Vector2* size);

    [LibraryImport("cimgui")]
    public static partial void axGetNodePosition(axNodeId nodeId, Vector2* pos);

    [LibraryImport("cimgui")]
    public static partial void axGetNodeSize(axNodeId nodeId, Vector2* sz);

    [LibraryImport("cimgui")]
    public static partial void axCenterNodeOnScreen(axNodeId nodeId);

    [LibraryImport("cimgui")]
    public static partial void
        axSetNodeZPosition(axNodeId nodeId,
            float z); // Sets node z position, nodes with higher value are drawn over nodes with lower value

    [LibraryImport("cimgui")]
    public static partial float axGetNodeZPosition(axNodeId nodeId); // Returns node z position, defaults is 0.0f

    [LibraryImport("cimgui")]
    public static partial void axRestoreNodeState(axNodeId nodeId);

    [LibraryImport("cimgui")]
    public static partial void axSuspend();

    [LibraryImport("cimgui")]
    public static partial void axResume();

    [LibraryImport("cimgui")]
    public static partial int axIsSuspended();

    [LibraryImport("cimgui")]
    public static partial int axIsActive();

    [LibraryImport("cimgui")]
    public static partial int axHasSelectionChanged();

    [LibraryImport("cimgui")]
    public static partial int axGetSelectedObjectCount();

    [LibraryImport("cimgui")]
    public static partial int axGetSelectedNodes(axNodeId* nodes, int size);

    [LibraryImport("cimgui")]
    public static partial int axGetSelectedLinks(axLinkId* links, int size);

    [LibraryImport("cimgui")]
    public static partial int axIsNodeSelected(axNodeId nodeId);

    [LibraryImport("cimgui")]
    public static partial int axIsLinkSelected(axLinkId linkId);

    [LibraryImport("cimgui")]
    public static partial void axClearSelection();

    [LibraryImport("cimgui")]
    public static partial void axSelectNode(axNodeId nodeId, int append);

    [LibraryImport("cimgui")]
    public static partial void axSelectLink(axLinkId linkId, int append);

    [LibraryImport("cimgui")]
    public static partial void axDeselectNode(axNodeId nodeId);

    [LibraryImport("cimgui")]
    public static partial void axDeselectLink(axLinkId linkId);

    [LibraryImport("cimgui")]
    public static partial int axDeleteNode(axNodeId nodeId);

    [LibraryImport("cimgui")]
    public static partial int axDeleteLink(axLinkId linkId);

    [LibraryImport("cimgui")]
    public static partial int axNodeHasAnyLinks(axNodeId nodeId); // Returns true if node has any link connected

    [LibraryImport("cimgui")]
    public static partial int axPinHasAnyLinks(axPinId pinId); // Return true if pin has any link connected

    [LibraryImport("cimgui")]
    public static partial int axNodeBreakLinks(axNodeId nodeId); // Break all links connected to this node

    [LibraryImport("cimgui")]
    public static partial int axPinBreakLinks(axPinId pinId); // Break all links connected to this pin

    [LibraryImport("cimgui")]
    public static partial void axNavigateToContent(float duration);

    [LibraryImport("cimgui")]
    public static partial void axNavigateToSelection(int zoomIn, float duration);

    [LibraryImport("cimgui")]
    public static partial int axShowNodeContextMenu(axNodeId* nodeId);

    [LibraryImport("cimgui")]
    public static partial int axShowPinContextMenu(axPinId* pinId);

    [LibraryImport("cimgui")]
    public static partial int axShowLinkContextMenu(axLinkId* linkId);

    [LibraryImport("cimgui")]
    public static partial int axShowBackgroundContextMenu();

    [LibraryImport("cimgui")]
    public static partial void axEnableShortcuts(int enable);

    [LibraryImport("cimgui")]
    public static partial int axAreShortcutsEnabled();

    [LibraryImport("cimgui")]
    public static partial int axBeginShortcut();

    [LibraryImport("cimgui")]
    public static partial int axAcceptCut();

    [LibraryImport("cimgui")]
    public static partial int axAcceptCopy();

    [LibraryImport("cimgui")]
    public static partial int axAcceptPaste();

    [LibraryImport("cimgui")]
    public static partial int axAcceptDuplicate();

    [LibraryImport("cimgui")]
    public static partial int axAcceptCreateNode();

    [LibraryImport("cimgui")]
    public static partial int axGetActionContextSize();

    [LibraryImport("cimgui")]
    public static partial int axGetActionContextNodes(axNodeId* nodes, int size);

    [LibraryImport("cimgui")]
    public static partial int axGetActionContextLinks(axLinkId* links, int size);

    [LibraryImport("cimgui")]
    public static partial void axEndShortcut();

    [LibraryImport("cimgui")]
    public static partial float axGetCurrentZoom();

    [LibraryImport("cimgui")]
    public static partial axNodeId axGetHoveredNode();

    [LibraryImport("cimgui")]
    public static partial axPinId axGetHoveredPin();

    [LibraryImport("cimgui")]
    public static partial axLinkId axGetHoveredLink();

    [LibraryImport("cimgui")]
    public static partial axNodeId axGetDoubleClickedNode();

    [LibraryImport("cimgui")]
    public static partial axPinId axGetDoubleClickedPin();

    [LibraryImport("cimgui")]
    public static partial axLinkId axGetDoubleClickedLink();

    [LibraryImport("cimgui")]
    public static partial int axIsBackgroundClicked();

    [LibraryImport("cimgui")]
    public static partial int axIsBackgroundDoubleClicked();

    [LibraryImport("cimgui")]
    public static partial int axGetBackgroundClickButtonIndex(); // -1 if none

    [LibraryImport("cimgui")]
    public static partial int axGetBackgroundDoubleClickButtonIndex(); // -1 if none

    [LibraryImport("cimgui")]
    public static partial int
        axGetLinkPins(axLinkId linkId, axPinId* startPinId,
            axPinId* endPinId); // pass nullptr if particular pin do not interest you

    [LibraryImport("cimgui")]
    public static partial int axPinHadAnyLinks(axPinId pinId);

    [LibraryImport("cimgui")]
    public static partial void axGetScreenSize(Vector2* size);

    [LibraryImport("cimgui")]
    public static partial void axScreenToCanvas(Vector2* pos, Vector2* canvas);

    [LibraryImport("cimgui")]
    public static partial void axCanvasToScreen(Vector2* pos, Vector2* screen);

    [LibraryImport("cimgui")]
    public static partial int axGetNodeCount();

    [LibraryImport("cimgui")]
    public static partial int axGetOrderedNodeIds(axNodeId* nodes, int size);
}
