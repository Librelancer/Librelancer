using System;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace LibreLancer.ImUI.NodeEditor;

using static NodeEditorNative;

public static unsafe class NodeEditor
{
    public static void SetCurrentEditor(NodeEditorContext ctx) => axSetCurrentEditor(ctx ?? IntPtr.Zero);

    public static NodeEditorContext GetCurrentEditor() => new NodeEditorContext(axGetCurrentEditor());

    public static Style* GetStyle() => axGetStyle();

    public static string GetStyleColorName(StyleColor styleColor) =>
        UnsafeHelpers.PtrToStringUTF8(axGetStyleColorName(styleColor));

    public static void PushStyleColor(StyleColor colorIndex, Color4 color)
    {
        Vector4 v = color;
        axPushStyleColor(colorIndex, &v);
    }

    public static void PopStyleColor(int count = 1) => axPopStyleColor(count);
    public static void PushStyleVar(StyleVar varIndex, float value) => axPushStyleVar1(varIndex, value);
    public static void PushStyleVar(StyleVar varIndex, Vector2 value) => axPushStyleVar2(varIndex, &value);
    public static void PushStyleVar(StyleVar varIndex, Vector4 value) => axPushStyleVar4(varIndex, &value);
    public static void PopStyleVar(int count = 1) => axPopStyleVar(count);

    public static void Begin(string id, Vector2 size = default)
    {
        var idptr = UnsafeHelpers.StringToHGlobalUTF8(id);
        axBegin(idptr, &size);
        Marshal.FreeHGlobal(idptr);
    }
    public static void End() => axEnd();

    public static void BeginNode(NodeId id) => axBeginNode(id);
    public static void BeginPin(PinId id, PinKind kind) => axBeginPin(id, kind);
    public static void PinRect(Vector2 a, Vector2 b) => axPinRect(&a, &b);
    public static void PinPivotRect(Vector2 a, Vector2 b) => axPinPivotRect(&a, &b);
    public static void PinPivotSize(Vector2 size) => axPinPivotSize(&size);
    public static void PinPivotScale(Vector2 scale) => axPinPivotScale(&scale);
    public static void PinPivotAlignment(Vector2 alignment) => axPinPivotAlignment(&alignment);
    public static void EndPin() => axEndPin();
    public static void Group(Vector2 size) => axGroup(&size);
    public static void EndNode() => axEndNode();

    public static bool BeginGroupHint(NodeId nodeId) => axBeginGroupHint(nodeId) != 0;
    public static Vector2 GetGroupMin()
    {
        var x = new Vector2();
        axGetGroupMin(&x);
        return x;
    }
    public static Vector2 GetGroupMax()
    {
        var x = new Vector2();
        axGetGroupMax(&x);
        return x;
    }
    public static ImDrawListPtr GetHintForegroundDrawList() => new ((IntPtr)axGetHintForegroundDrawList());
    public static ImDrawListPtr GetHintBackgroundDrawList() => new ((IntPtr)axGetHintBackgroundDrawList());
    public static void EndGroupHint() => axEndGroupHint();

    public static ImDrawListPtr GetNodeBackgroundDrawList(NodeId nodeId) => new((IntPtr)axGetNodeBackgroundDrawList(nodeId));

    public static bool Link(LinkId id, PinId startPinId, PinId endPinId, Color4? color = null, float thickness = 1.0f)
    {
        Vector4 v = color ?? Color4.White;
        return axLink(id, startPinId, endPinId, &v, thickness) != 0;
    }

    public static void Flow(LinkId id, FlowDirection direction = FlowDirection.Forward) => axFlow(id, direction);

    public static bool BeginCreate(Color4? color = null, float thickness = 1.0f)
    {
        Vector4 v = color ?? Color4.White;
        return axBeginCreate(&v, thickness) != 0;
    }

    public static bool QueryNewLink(out PinId startId, out PinId endId)
    {
        fixed (PinId* a = &startId, b = &endId)
            return axQueryNewLink((IntPtr*)a, (IntPtr*)b) != 0;
    }

    public static bool QueryNewLink(out PinId startId, out PinId endId, Color4 color, float thickness = 1.0f)
    {
        Vector4 v = color;
        fixed (PinId* a = &startId, b = &endId)
            return axQueryNewLink_Styled((IntPtr*)a, (IntPtr*)b, &v, thickness) != 0;
    }

    public static bool QueryNewNode(out PinId pinId)
    {
        fixed (PinId* a = &pinId)
            return axQueryNewNode((IntPtr*)a) != 0;
    }

    public static bool QueryNewNode(out PinId pinId, Color4 color, float thickness = 1.0f)
    {
        Vector4 v = color;
        fixed (PinId* a = &pinId)
            return axQueryNewNode_Styled((IntPtr*)a, &v, thickness) != 0;
    }

    public static bool AcceptNewItem() => axAcceptNewItem() != 0;

    public static bool AcceptNewItem(Color4 color, float thickness = 1.0f)
    {
        Vector4 v = color;
        return axAcceptNewItem_Styled(&v, thickness) != 0;
    }

    public static void RejectNewItem() => axRejectNewItem();

    public static void RejectNewItem(Color4 color, float thickness = 1.0f)
    {
        Vector4 v = color;
        axRejectNewItem_Styled(&v, thickness);
    }

    public static void EndCreate() => axEndCreate();

    public static bool BeginDelete() => axBeginDelete() != 0;

    public static bool QueryDeletedLink(out LinkId linkId)
    {
        fixed (LinkId* a = &linkId)
            return axQueryDeletedLink((IntPtr*)a, null, null) != 0;
    }

    public static bool QueryDeletedLink(out LinkId linkId, out PinId startId)
    {
        fixed (LinkId* a = &linkId)
        fixed (PinId* b = &startId)
            return axQueryDeletedLink((IntPtr*)a, (IntPtr*)b, null) != 0;
    }

    public static bool QueryDeletedLink(out LinkId linkId, out PinId startId, out PinId endId)
    {
        fixed (LinkId* a = &linkId)
        fixed (PinId* b = &startId, c= &endId)
            return axQueryDeletedLink((IntPtr*)a, (IntPtr*)b, (IntPtr*)c) != 0;
    }

    public static bool QueryDeletedNode(out NodeId nodeId)
    {
        fixed (NodeId* a = &nodeId)
            return axQueryDeletedNode((IntPtr*)a) != 0;
    }
    public static bool AcceptDeletedItem(bool deleteDependencies = true) =>
        axAcceptDeletedItem(deleteDependencies ? 1 : 0) != 0;
    public static void RejectDeletedItem() => axRejectDeletedItem();
    public static void EndDelete() => axEndDelete();

    public static void SetNodePosition(NodeId nodeId, Vector2 editorPosition) =>
        axSetNodePosition(nodeId, &editorPosition);

    public static void SetGroupSize(NodeId nodeId, Vector2 size) =>
        axSetGroupSize(nodeId, &size);

    public static Vector2 GetNodePosition(NodeId id)
    {
        var x = new Vector2();
        axGetNodePosition(id, &x);
        return x;
    }

    public static Vector2 GetNodeSize(NodeId id)
    {
        var x = new Vector2();
        axGetNodeSize(id, &x);
        return x;
    }

    public static void CenterNodeOnScreen(NodeId nodeId) => axCenterNodeOnScreen(nodeId);
    public static void SetNodeZPosition(NodeId nodeId, float z) => axSetNodeZPosition(nodeId, z);
    public static float GetNodeZPosition(NodeId nodeId) => axGetNodeZPosition(nodeId);

    public static void RestoreNodeState(NodeId nodeId) => axRestoreNodeState(nodeId);

    public static void Suspend() => axSuspend();
    public static void Resume() => axResume();
    public static bool IsSuspended() => axIsSuspended() != 0;

    public static bool IsActive() => axIsActive() != 0;

    public static bool HasSelectionChanged() => axHasSelectionChanged() != 0;
    public static int GetSelectedObjectCount() => axGetSelectedObjectCount();

    public static int GetSelectedNodes(Span<NodeId> nodes)
    {
        fixed (NodeId* ptr = &nodes.GetPinnableReference())
            return axGetSelectedNodes((IntPtr*)ptr, nodes.Length);
    }

    public static int GetSelectedLinks(Span<LinkId> links)
    {
        fixed (LinkId* ptr = &links.GetPinnableReference())
            return axGetSelectedLinks((IntPtr*)ptr, links.Length);
    }

    public static bool IsNodeSelected(NodeId nodeId) => axIsNodeSelected(nodeId) != 0;
    public static bool IsLinkSelected(LinkId linkId) => axIsLinkSelected(linkId) != 0;
    public static void ClearSelection() => axClearSelection();
    public static void SelectNode(NodeId nodeId, bool append = false) => axSelectNode(nodeId, append ? 1 : 0);
    public static void SelectLink(LinkId linkId, bool append = false) => axSelectLink(linkId, append ? 1 : 0);
    public static void DeselectNode(NodeId nodeId) => axDeselectNode(nodeId);
    public static void DeselectLink(LinkId linkId) => axDeselectLink(linkId);

    public static bool DeleteNode(NodeId nodeId) => axDeleteNode(nodeId) != 0;
    public static bool DeleteLink(LinkId linkId) => axDeleteLink(linkId) != 0;

    public static bool HasAnyLinks(NodeId nodeId) => axNodeHasAnyLinks(nodeId) != 0;
    public static bool HasAnyLinks(PinId pinId) => axPinHasAnyLinks(pinId) != 0;
    public static int BreakLinks(NodeId nodeId) => axNodeBreakLinks(nodeId);
    public static int BreakLinks(PinId pinId) => axPinBreakLinks(pinId);

    public static void NavigateToContent(float duration = -1) => axNavigateToContent(duration);
    public static void NavigateToSelection(bool zoomIn = false, float duration = -1) => axNavigateToSelection(zoomIn ? 1 : 0, duration);

    public static bool ShowNodeContextMenu(out NodeId nodeId)
    {
        fixed (NodeId* a = &nodeId)
            return axShowNodeContextMenu((IntPtr*)a) != 0;
    }

    public static bool ShowPinContextMenu(out PinId pinId)
    {
        fixed (PinId* a = &pinId)
            return axShowPinContextMenu((IntPtr*)a) != 0;
    }

    public static bool ShowLinkContextMenu(out LinkId linkId)
    {
        fixed (LinkId* a = &linkId)
            return axShowLinkContextMenu((IntPtr*)a) != 0;
    }

    public static bool ShowBackgroundContextMenu() => axShowBackgroundContextMenu() != 0;

    public static void EnableShortcuts(bool enable) => axEnableShortcuts(enable ? 1 : 0);
    public static bool AreShortcutsEnabled() => axAreShortcutsEnabled() != 0;

    public static bool BeginShortcut() => axBeginShortcut() != 0;
    public static bool AcceptCut() => axAcceptCut() != 0;
    public static bool AcceptCopy() => axAcceptCopy() != 0;
    public static bool AcceptPaste() => axAcceptPaste() != 0;
    public static bool AcceptDuplicate() => axAcceptDuplicate() != 0;
    public static bool AcceptCreateNode() => axAcceptCreateNode() != 0;
    public static int GetActionContextSize() => axGetActionContextSize();

    public static int GetActionContextNodes(Span<NodeId> nodes)
    {
        fixed (NodeId* a = &nodes.GetPinnableReference())
            return axGetActionContextNodes((IntPtr*)a, nodes.Length);
    }

    public static int GetActionContextLinks(Span<LinkId> links)
    {
        fixed (LinkId* l = &links.GetPinnableReference())
            return axGetActionContextLinks((IntPtr*)l, links.Length);
    }

    public static void EndShortcut() => axEndShortcut();

    public static float GetCurrentZoom() => axGetCurrentZoom();

    public static NodeId GetHoveredNode() => axGetHoveredNode();
    public static PinId GetHoveredPin() => axGetHoveredPin();
    public static LinkId GetHoveredLink() => axGetHoveredLink();
    public static NodeId GetDoubleClickedNode() => axGetDoubleClickedNode();
    public static PinId GetDoubleClickedPin() => axGetDoubleClickedPin();
    public static LinkId GetDoubleClickedLink() => axGetDoubleClickedLink();
    public static bool IsBackgroundClicked() => axIsBackgroundClicked() != 0;
    public static bool IsBackgroundDoubleClicked() => axIsBackgroundDoubleClicked() != 0;
    public static ImGuiMouseButton GetBackgroundClickButtonIndex() => (ImGuiMouseButton)axGetBackgroundClickButtonIndex(); // -1 if none
    public static ImGuiMouseButton GetBackgroundDoubleClickButtonIndex() => (ImGuiMouseButton)axGetBackgroundDoubleClickButtonIndex(); // -1 if none

    public static bool GetLinkPins(LinkId linkId) => axGetLinkPins(linkId, null, null) != 0;

    public static bool GetLinkPins(LinkId linkId, out PinId startPinId)
    {
        fixed (PinId* a = &startPinId)
            return axGetLinkPins(linkId, (IntPtr*)a, null) != 0;
    }

    public static bool GetLinkPins(LinkId linkId, out PinId startPinId, out PinId endPinId)
    {
        fixed (PinId* a = &startPinId, b = &endPinId)
            return axGetLinkPins(linkId, (IntPtr*)a, (IntPtr*)b) != 0;
    }
    public static bool PinHadAnyLinks(PinId pinId) => axPinHadAnyLinks(pinId) != 0;

    public static Vector2 ScreenToCanvas(Vector2 pos)
    {
        Vector2 x;
        axScreenToCanvas(&pos, &x);
        return x;
    }
    public static Vector2 CanvasToScreen(Vector2 pos)
    {
        Vector2 x;
        axCanvasToScreen(&pos, &x);
        return x;
    }

    public static int GetNodeCount() => axGetNodeCount();

    public static int GetOrderedNodeIds(Span<NodeId> nodes)
    {
        fixed (NodeId* a = &nodes.GetPinnableReference())
            return axGetOrderedNodeIds((IntPtr*)a, nodes.Length);
    }
}
