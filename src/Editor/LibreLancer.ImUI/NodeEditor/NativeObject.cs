using System;

namespace LibreLancer.ImUI.NodeEditor;

public abstract class NativeObject
{
    public IntPtr Handle;
    public static implicit operator IntPtr(NativeObject self) => self.Handle;
}
