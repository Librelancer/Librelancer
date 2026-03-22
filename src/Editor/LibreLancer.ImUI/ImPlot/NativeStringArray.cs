using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LibreLancer.ImUI.ImPlot;

unsafe class NativeStringArray : IDisposable
{
    public IntPtr Pointer;
    private List<IntPtr> toDispose = new();

    public NativeStringArray(string?[]? values)
    {
        if (values == null)
            return;
        Pointer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(IntPtr)) * (values.Length < 1 ? 1 : values.Length));
        IntPtr* ptrs = (IntPtr*)Pointer;
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == null)
                ptrs[i] = IntPtr.Zero;
            else
            {
                var p = Marshal.StringToCoTaskMemUTF8(values[i]);
                toDispose.Add(p);
                ptrs[i] = p;
            }
        }
        toDispose.Add(Pointer);
    }

    public void Dispose()
    {
        foreach(var p in toDispose)
            Marshal.FreeCoTaskMem(p);
        toDispose.Clear();
        Pointer = IntPtr.Zero;
    }
}
