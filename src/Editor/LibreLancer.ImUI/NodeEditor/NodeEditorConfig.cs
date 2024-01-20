using System;
using System.Runtime.InteropServices;
namespace LibreLancer.ImUI.NodeEditor;

using static NodeEditorNative;

public delegate void ConfigSession(IntPtr userPointer);
public delegate bool ConfigSaveSettings(IntPtr data, UIntPtr size, SaveReasonFlags reason, IntPtr userPointer);
public delegate UIntPtr ConfigLoadSettings(IntPtr data, IntPtr userPointer);
public delegate bool ConfigSaveNodeSettings(NodeId nodeId, IntPtr data, UIntPtr size, SaveReasonFlags reason,
    IntPtr userPointer);
public delegate UIntPtr ConfigLoadNodeSettings(NodeId nodeId, IntPtr data, IntPtr userPointer);
public class NodeEditorConfig : NativeObject, IDisposable
{
    private ConfigSession beginSave;
    private ConfigSession endSave;
    private ConfigSaveSettings saveSettings;
    private ConfigLoadSettings loadSettings;
    private Func<IntPtr, IntPtr, UIntPtr, SaveReasonFlags, IntPtr, int> saveNodeSettings;
    private Func<IntPtr, IntPtr, IntPtr, UIntPtr> loadNodeSettings;


    public NodeEditorConfig()
    {
        Handle = axConfigNew();
    }

    public void SetBeginSaveSession(ConfigSession cb)
    {
        beginSave = cb;
        axConfig_set_BeginSaveSession(Handle, Marshal.GetFunctionPointerForDelegate(cb));
    }

    public void SetEndSaveSession(ConfigSession cb)
    {
        endSave = cb;
        axConfig_set_EndSaveSession(Handle, Marshal.GetFunctionPointerForDelegate(cb));
    }

    public void SetSaveSettings(ConfigSaveSettings cb)
    {
        saveSettings = cb;
        axConfig_set_SaveSettings(Handle, Marshal.GetFunctionPointerForDelegate(cb));
    }

    public void SetLoadSettings(ConfigLoadSettings cb)
    {
        loadSettings = cb;
        axConfig_set_LoadSettings(Handle, Marshal.GetFunctionPointerForDelegate(cb));
    }

    public void SetSaveNodeSettings(ConfigSaveNodeSettings cb)
    {
        saveNodeSettings = (a, b, c, d, e) =>
            cb(a, b, c, d, e) ? 1 : 0;
        axConfig_set_SaveNodeSettings(Handle, Marshal.GetFunctionPointerForDelegate(cb));
    }

    public void SetLoadNodeSettings(ConfigLoadNodeSettings cb)
    {
        loadNodeSettings = (a, b, c) => cb(a, b, c);
        axConfig_set_LoadNodeSettings(Handle, Marshal.GetFunctionPointerForDelegate(cb));
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
