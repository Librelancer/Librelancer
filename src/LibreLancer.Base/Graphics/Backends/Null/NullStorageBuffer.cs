using System;
using System.Runtime.InteropServices;

namespace LibreLancer.Graphics.Backends.Null;

public class NullStorageBuffer : IStorageBuffer
{
    private NativeBuffer buffer;
    private int size;

    public NullStorageBuffer(int size, int stride)
    {
        buffer = UnsafeHelpers.Allocate(size * stride);
    }

    public void Dispose()
    {
        buffer.Dispose();
    }

    public int GetAlignedIndex(int input) => input;

    public void SetData<T>(T[] array, int start = 0, int length = -1) where T : unmanaged
    {
    }

    public void SetData<T>(ref T item, int index = 0) where T : unmanaged
    {
    }

    public void BindTo(int binding, int start = 0, int count = 0)
    {
    }

    public unsafe ref T Data<T>(int i) where T : unmanaged
    {
        if (i >= size) throw new IndexOutOfRangeException();
        return ref (((T*)(IntPtr)buffer)!)[i];
    }

    public IntPtr BeginStreaming()
    {
        return (IntPtr)buffer;
    }

    public void EndStreaming(int count)
    {
    }
}
