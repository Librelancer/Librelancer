using System;

namespace LibreLancer.Graphics.Backends.OpenGL;

class GLStorageBuffer : GLCycledBuffer, IStorageBuffer
{
    private IntPtr mapping;
    private int stride;
    private int size;
    private GLRenderContext ctx;

    public GLStorageBuffer(int size, int stride, GLRenderContext ctx) : base(ctx)
    {
        this.stride = stride;
        this.size = size;
        this.ctx = ctx;
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
        ctx.BindToIndex(GL.GL_SHADER_STORAGE_BUFFER, binding, startPtr, length, this);
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
        ActiveIdx = GetNextBuffer();
        GL.BindBuffer(GL.GL_SHADER_STORAGE_BUFFER, IDs[ActiveIdx]);
        mapping = GL.MapBufferRange(GL.GL_SHADER_STORAGE_BUFFER, 0, size * stride,
            GL.GL_MAP_WRITE_BIT | GL.GL_MAP_UNSYNCHRONIZED_BIT);
        return mapping;
    }

    public void EndStreaming(int count)
    {
        if (mapping == IntPtr.Zero) throw new InvalidOperationException("Not mapped!");
        mapping = IntPtr.Zero;
        GL.BindBuffer(GL.GL_SHADER_STORAGE_BUFFER, IDs[ActiveIdx]);
        GL.UnmapBuffer(GL.GL_SHADER_STORAGE_BUFFER);
    }

    protected override uint GenerateBuffer()
    {
        var id = GL.GenBuffer();
        GL.BindBuffer(GL.GL_SHADER_STORAGE_BUFFER, id);
        GL.BufferData(GL.GL_SHADER_STORAGE_BUFFER, new IntPtr(size * stride), IntPtr.Zero, GL.GL_DYNAMIC_DRAW);
        return id;
    }
}
