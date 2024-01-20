using System;

namespace LibreLancer.ImUI.NodeEditor;

public class NodeEditorContext : NativeObject, IDisposable
{
    public NodeEditorContext(NodeEditorConfig config) =>
        Handle = NodeEditorNative.axCreateEditor(config);

    internal NodeEditorContext(IntPtr handle) =>
        Handle = handle;

    public NodeEditorConfig GetConfig() => new NodeEditorConfig(NodeEditorNative.axGetConfig(this));
    public void Dispose() =>
        NodeEditorNative.axDestroyEditor(Handle);
}
