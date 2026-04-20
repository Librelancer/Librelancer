// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;

namespace LibreLancer.Graphics.Backends.OpenGL;

internal class GLStorageBuffer : IStorageBuffer
{
    private Type storageType;
    private uint ID;
    private int stride;
    private int size;
    private int gAlignment;
    private IntPtr mapping;

    public GLStorageBuffer(int size, int stride, Type type)
    {
        if (stride % 16 != 0)
        {
            throw new Exception("Must be aligned to minimum 16");
        }

        this.stride = stride;
        this.size = size;

        storageType = type;
        ID = GL.GenBuffer();
        GLBind.UniformBuffer(ID);
        GL.BufferData(GL.GL_UNIFORM_BUFFER, new IntPtr(size * stride), IntPtr.Zero, GL.GL_STREAM_DRAW);
        GL.GetIntegerv(GL.GL_UNIFORM_BUFFER_OFFSET_ALIGNMENT, out int align);

        var lval = stride % align;
        gAlignment = lval == 0 ? 0 : align;
#if DEBUG
            if(align < 256 && 256 % align != 0)
                gAlignment = 256; //Set larger alignment on debug for testing
#endif
    }

    public int GetAlignedIndex(int input)
    {
        if (gAlignment == 0)
        {
            return input;
        }

        int offset = input * stride;
        var aOffset = (offset + (gAlignment - 1)) & ~(gAlignment - 1);
        return aOffset / stride;
    }

    public void BindTo(int binding, int start = 0, int count = 0)
    {
        if (GetAlignedIndex(start) != start)
        {
            throw new InvalidOperationException("Uniform buffer alignment error");
        }
        var startPtr = (IntPtr) (start * stride);
        var length = (IntPtr) ((count <= 0 ? size : count) * stride);
        if ((long) startPtr + (long) length > (size * stride))
            throw new IndexOutOfRangeException();
        GL.BindBufferRange(GL.GL_UNIFORM_BUFFER, (uint) binding, ID, startPtr, length);
    }

    public unsafe ref T Data<T>(int i) where T : unmanaged
    {
        if (i >= size)
        {
            throw new IndexOutOfRangeException();
        }
        if (mapping == IntPtr.Zero)
        {
            throw new InvalidOperationException();
        }

        return ref (((T*) (IntPtr) mapping!)!)[i];
    }

    // NOTE: On buffer orphaning.
    // We use MAP_VALIDATE_BUFFER_BIT instead of glBufferData NULL because
    // under renderdoc, and Haswell iGPU - glBufferData NULL takes
    // a very long time to return, and doesn't seem to simply orphan the buffer.

    public IntPtr BeginStreaming()
    {
        if (mapping != IntPtr.Zero) throw new InvalidOperationException("Already mapped!");
        GLBind.UniformBuffer(ID);
        mapping = GL.MapBufferRange(GL.GL_UNIFORM_BUFFER, 0, size * stride,
            GL.GL_MAP_INVALIDATE_BUFFER_BIT | GL.GL_MAP_WRITE_BIT);
        return mapping;
    }

    public unsafe void EndStreaming(int count)
    {
        if (mapping == IntPtr.Zero) throw new InvalidOperationException("Not mapped!");
        mapping = IntPtr.Zero;
        GLBind.UniformBuffer(ID);
        GL.UnmapBuffer(GL.GL_UNIFORM_BUFFER);
    }

    public void Dispose()
    {
        GL.DeleteBuffers(1, ref ID);
    }
}
