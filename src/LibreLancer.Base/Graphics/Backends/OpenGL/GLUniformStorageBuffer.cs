// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Graphics.Backends.OpenGL;

internal sealed class GLUniformStorageBuffer : GLCycledBuffer, IStorageBuffer
{
    private int stride;
    private int size;
    private int gAlignment;
    private IntPtr mapping;
    private GLRenderContext ctx;

    public GLUniformStorageBuffer(int size, int stride, GLRenderContext ctx) : base(ctx)
    {
        if (stride % 16 != 0)
        {
            throw new Exception("Must be aligned to minimum 16");
        }

        this.stride = stride;
        this.size = size;
        this.ctx = ctx;

        AllocateBufferIndex(0);

        GL.GetIntegerv(GL.GL_UNIFORM_BUFFER_OFFSET_ALIGNMENT, out int align);

        var lval = stride % align;
        gAlignment = lval == 0 ? 0 : align;
#if DEBUG
        if (align < 256 && 256 % align != 0)
            gAlignment = 256; //Set larger alignment on debug for testing
#endif
    }

    protected override uint GenerateBuffer()
    {
        var id = GL.GenBuffer();
        GLBind.UniformBuffer(id);
        GL.BufferData(GL.GL_UNIFORM_BUFFER, new IntPtr(size * stride), IntPtr.Zero, GL.GL_DYNAMIC_DRAW);
        return id;
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

        var startPtr = (IntPtr)(start * stride);
        var length = (IntPtr)((count <= 0 ? size : count) * stride);
        if ((long)startPtr + (long)length > (size * stride))
            throw new IndexOutOfRangeException();

        ctx.BindToIndex(GL.GL_UNIFORM_BUFFER, binding, startPtr, length, this);
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

        return ref ((T*)mapping)[i];
    }


    // GL_MAP_INVALIDATE_BUFFER_BIT causes slowdowns on intel hardware.
    // Use buffer cycling when possible, GL_ARB_sync not guaranteed to be supported.
    public IntPtr BeginStreaming()
    {
        if (mapping != IntPtr.Zero) throw new InvalidOperationException("Already mapped!");
        if (ctx.FencedUBO)
        {
            ActiveIdx = GetNextBuffer();
            GLBind.UniformBuffer(IDs[ActiveIdx]);
            mapping = GL.MapBufferRange(GL.GL_UNIFORM_BUFFER, 0, size * stride,
                GL.GL_MAP_WRITE_BIT | GL.GL_MAP_UNSYNCHRONIZED_BIT);
        }
        else
        {
            GLBind.UniformBuffer(IDs[ActiveIdx]);
            mapping = GL.MapBufferRange(GL.GL_UNIFORM_BUFFER, 0, size * stride,
                GL.GL_MAP_WRITE_BIT);
        }

        return mapping;
    }

    public void EndStreaming(int count)
    {
        if (mapping == IntPtr.Zero) throw new InvalidOperationException("Not mapped!");
        mapping = IntPtr.Zero;
        GLBind.UniformBuffer(IDs[ActiveIdx]);
        GL.UnmapBuffer(GL.GL_UNIFORM_BUFFER);
    }
}
