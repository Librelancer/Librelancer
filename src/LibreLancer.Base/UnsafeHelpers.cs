// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

namespace LibreLancer;

public class NativeBuffer : IDisposable
{
    public readonly IntPtr Handle;
    public readonly nint Size;
    public bool IsDisposed { get; private set; }

    private string allocator;

    internal NativeBuffer(IntPtr handle, nint size, string allocator)
    {
        Handle = handle;
        Size = size;
        this.allocator = allocator;
    }

    public void Dispose()
    {
        Interlocked.Add(ref UnsafeHelpers.InternalAllocated, -Size);
        Marshal.FreeHGlobal(Handle);
        IsDisposed = true;
    }

    public static explicit operator IntPtr(NativeBuffer buffer)
    {
        return buffer?.Handle ?? IntPtr.Zero;
    }

    public static unsafe explicit operator byte*(NativeBuffer buffer)
    {
        return (byte*)(buffer?.Handle ?? IntPtr.Zero);
    }

    ~NativeBuffer()
    {
        if (IsDisposed)
        {
            return;
        }

        FLLog.Debug("WARNING", $"NativeBuffer leak, allocated by '{allocator}'");
        Dispose();
    }
}

public static class UnsafeHelpers
{
    internal static long InternalAllocated;
    public static long Allocated => InternalAllocated;

    public static NativeBuffer Allocate(int size, [CallerFilePath] string callerName = "") => Allocate((nint)size, callerName);

    public static NativeBuffer Allocate(nint size, [CallerFilePath] string callerName = "")
    {
        var mem = Marshal.AllocHGlobal(size);
        Interlocked.Add(ref InternalAllocated, size);
        return new NativeBuffer(mem, size, callerName);
    }

    public static byte[] CastArray<T>(T[] src) where T : unmanaged
    {
        return MemoryMarshal.AsBytes(src.AsSpan()).ToArray();
    }
    public static unsafe string? PtrToStringUTF8(IntPtr intptr, int maxBytes = int.MaxValue)
    {
        int i = 0;
        var ptr = (byte*)intptr;
        while (ptr[i] != 0)
        {
            i++;
            if (i >= maxBytes) break;
        }

        var bytes = new byte[i];
        Marshal.Copy(intptr, bytes, 0, i);
        return Encoding.UTF8.GetString(bytes);
    }

    public static unsafe NativeBuffer StringToNativeUTF16(string str)
    {
        var bytes = Encoding.Unicode.GetBytes(str);
        var ptr = Allocate(bytes.Length + 2);
        Marshal.Copy(bytes, 0, ptr.Handle, bytes.Length);
        ((byte*)ptr)[bytes.Length] = 0;
        ((byte*)ptr)[bytes.Length + 1] = 0;
        return ptr;
    }

    public static unsafe NativeBuffer StringToNativeUTF8(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        var ptr = Allocate(bytes.Length + 1);
        Marshal.Copy(bytes, 0, ptr.Handle, bytes.Length);
        ((byte*)ptr)[bytes.Length] = 0;
        return ptr;
    }
}

public ref struct UTF8ZHelper
{
    private byte[]? poolArray;
    private Span<byte> bytes;
    private readonly Span<byte> utf8z;
    private bool used;

    public UTF8ZHelper(Span<byte> initialBuffer, ReadOnlySpan<char> value)
    {
        poolArray = null;
        bytes = initialBuffer;
        used = false;
        int maxSize = Encoding.UTF8.GetMaxByteCount(value.Length) + 1;

        if (bytes.Length < maxSize)
        {
            poolArray = ArrayPool<byte>.Shared.Rent(maxSize);
            bytes = new Span<byte>(poolArray);
        }

        int byteCount = Encoding.UTF8.GetBytes(value, bytes);
        bytes[byteCount] = 0;
        utf8z = bytes[..(byteCount + 1)];
    }

    public Span<byte> ToUTF8Z()
    {
        return utf8z;
    }

    public void Dispose()
    {
        var toReturn = poolArray;

        if (toReturn == null)
        {
            return;
        }

        poolArray = null;
        ArrayPool<byte>.Shared.Return(toReturn);
    }
}
