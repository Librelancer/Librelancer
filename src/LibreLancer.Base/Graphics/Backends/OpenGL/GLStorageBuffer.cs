using System;
using LibreLancer.Graphics.Backends.OpenGL;

namespace LibreLancer.Graphics.Backends.OpenGL;

class GLStorageBuffer : IStorageBuffer
{
    private uint ID;
    private IntPtr mapping;
    private GLRenderContext ctx;
    private int stride;
    private int size;


    public GLStorageBuffer(int size, int stride, GLRenderContext ctx)
    {
        this.stride = stride;
        this.size = size;
        this.ctx = ctx;
        ID = GL.GenBuffer();
        GL.BindBuffer(GL.GL_SHADER_STORAGE_BUFFER, ID);
        GL.BufferData(GL.GL_SHADER_STORAGE_BUFFER, new IntPtr(size * stride), IntPtr.Zero, GL.GL_DYNAMIC_DRAW);
    }


    public void Dispose()
    {
        // TODO release managed resources here
    }

    public int GetAlignedIndex(int input)
    {
        int offset = input * stride;
        var aOffset = (offset + (4 - 1)) & ~(4 - 1);
        return aOffset / stride;
    }

    public void BindTo(int binding, int start = 0, int count = 0)
    {
        var startPtr = (IntPtr)(start * stride);
        var length = (IntPtr)((count <= 0 ? size : count) * stride);
        if ((long)startPtr + (long)length > (size * stride))
            throw new IndexOutOfRangeException();
        GL.BindBufferRange(GL.GL_SHADER_STORAGE_BUFFER, (uint)binding, ID, startPtr, length);
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

    public IntPtr BeginStreaming()
    {
        if (mapping != IntPtr.Zero) throw new InvalidOperationException("Already mapped!");
        GL.BindBuffer(GL.GL_SHADER_STORAGE_BUFFER, ID);
        mapping = GL.MapBufferRange(GL.GL_SHADER_STORAGE_BUFFER, 0, size * stride,
            GL.GL_MAP_WRITE_BIT | GL.GL_MAP_INVALIDATE_BUFFER_BIT);
        return mapping;
    }

    public void EndStreaming(int count)
    {
        if (mapping == IntPtr.Zero) throw new InvalidOperationException("Not mapped!");
        mapping = IntPtr.Zero;
        GL.BindBuffer(GL.GL_SHADER_STORAGE_BUFFER, ID);
        GL.UnmapBuffer(GL.GL_SHADER_STORAGE_BUFFER);
    }
}
