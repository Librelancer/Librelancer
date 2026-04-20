// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;

namespace LibreLancer.Graphics.Backends.OpenGL;

internal class GLStorageBuffer : IStorageBuffer
{
    private Type storageType;
    private int stride;
    private int size;
    private int gAlignment;
    private IntPtr mapping;
    private GLRenderContext ctx;

    private bool allAllocated = false;
    internal IntPtr[] Fences = [0, 0, 0];
    internal uint[] IDs = [0, 0, 0];
    internal int ActiveIdx = 0;


    public GLStorageBuffer(int size, int stride, Type type, GLRenderContext ctx)
    {
        if (stride % 16 != 0)
        {
            throw new Exception("Must be aligned to minimum 16");
        }

        this.stride = stride;
        this.size = size;
        this.ctx = ctx;

        storageType = type;
        AllocateBufferIndex(0);

        GL.GetIntegerv(GL.GL_UNIFORM_BUFFER_OFFSET_ALIGNMENT, out int align);

        var lval = stride % align;
        gAlignment = lval == 0 ? 0 : align;
#if DEBUG
        if (align < 256 && 256 % align != 0)
            gAlignment = 256; //Set larger alignment on debug for testing
#endif
    }

    void AllocateBufferIndex(int idx)
    {
        IDs[idx] = GL.GenBuffer();
        GLBind.UniformBuffer(IDs[idx]);
        GL.BufferData(GL.GL_UNIFORM_BUFFER, new IntPtr(size * stride), IntPtr.Zero, GL.GL_DYNAMIC_DRAW);
        allAllocated = true;
        for (int i = 0; i < IDs.Length; i++)
        {
            if (IDs[i] == 0)
            {
                allAllocated = false;
                break;
            }
        }
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

        ctx.BindToIndex(binding, startPtr, length, this);
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

    int GetNextBuffer()
    {
        for (int i = 0; i < 3; i++)
        {
            int idx = (ActiveIdx + i) % 3;
            if (IDs[idx] == 0)
                continue;
            if (Fences[idx] == 0)
            {
                return idx;
            }

            var r = GL.ClientWaitSync(Fences[idx], 0, 0);
            if (r == GL.GL_CONDITION_SATISFIED ||
                r == GL.GL_ALREADY_SIGNALED)
            {
                GL.DeleteSync(Fences[i]);
                Fences[i] = 0;
                return idx;
            }
        }

        if (allAllocated)
        {
            int idx = (ActiveIdx + 1);
            uint r;
            do
            {
                r = GL.ClientWaitSync(Fences[idx], 0, 1000);
            } while (r != GL.GL_CONDITION_SATISFIED && r != GL.GL_ALREADY_SIGNALED);

            GL.DeleteSync(Fences[idx]);
            Fences[idx] = 0;
            return idx;
        }

        for (int i = 0; i < IDs.Length; i++)
        {
            if (IDs[i] == 0)
            {
                AllocateBufferIndex(i);
                return i;
            }
        }

        throw new InvalidOperationException("Unreachable");
    }


    // NOTE: Buffer orphaning doesn't seem to work for GL_UNIFORM_BUFFER
    // under intel or renderdoc. Do some fancy work instead
    public IntPtr BeginStreaming()
    {
        if (mapping != IntPtr.Zero) throw new InvalidOperationException("Already mapped!");
        if (ctx.UseFencedUBO)
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

    public void Dispose()
    {
        for (int i = 0; i < ctx.BoundBuffers.Length; i++)
        {
            if (ctx.BoundBuffers[i] == this)
            {
                ctx.BoundBuffers[i] = null;
            }
        }

        for (int i = 0; i < Fences.Length; i++)
        {
            if (Fences[i] != 0)
                GL.DeleteSync(Fences[i]);
        }

        for (int i = 0; i < IDs.Length; i++)
        {
            if (IDs[i] != 0)
                GL.DeleteBuffer(IDs[i]);
        }
    }
}
