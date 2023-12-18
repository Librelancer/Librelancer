using System;
using System.Runtime.InteropServices;
namespace LibreLancer.ImUI.NodeEditor;

using static NodeEditorNative;

public class NodeEditorConfig : NativeObject, IDisposable
{
    public NodeEditorConfig()
    {
        Handle = axConfigNew();
    }

    private IntPtr lastSet = IntPtr.Zero;

    public string SettingsFile
    {
        get => UnsafeHelpers.PtrToStringUTF8(axConfig_get_SettingsFile(Handle));
        set
        {
            if(lastSet != 0) Marshal.FreeHGlobal(lastSet);
            lastSet = UnsafeHelpers.StringToHGlobalUTF8(value);
            axConfig_set_SettingsFile(Handle, lastSet);
        }
    }

    public CanvasSizeMode CanvasSizeMode
    {
        get => axConfig_get_CanvasSizeMode(Handle);
        set => axConfig_set_CanvasSizeMode(Handle, value);
    }

    public int DragButtonIndex
    {
        get => axConfig_get_DragButtonIndex(Handle);
        set => axConfig_set_DragButtonIndex(Handle, value);
    }

    public int SelectButtonIndex
    {
        get => axConfig_get_SelectButtonIndex(Handle);
        set => axConfig_set_SelectButtonIndex(Handle, value);
    }

    public int NavigateButtonIndex
    {
        get => axConfig_get_NavigateButtonIndex(Handle);
        set => axConfig_set_NavigateButtonIndex(Handle, value);
    }

    public int ContextMenuButtonIndex
    {
        get => axConfig_get_ContextMenuButtonIndex(Handle);
        set => axConfig_set_ContextMenuButtonIndex(Handle, value);
    }

    internal NodeEditorConfig(IntPtr handle)
    {
        Handle = handle;
    }

    public void Dispose()
    {
        axConfigFree(Handle);
        if(lastSet != 0)
            Marshal.FreeHGlobal(lastSet);
    }
}
