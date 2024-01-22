// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics.Backends.OpenGL;

namespace LibreLancer.Graphics.Backends.OpenGL
{
    class GLShaderStorageBuffer : IShaderStorageBuffer
    {
        public int Size { get; private set; }
        uint ID;
        public GLShaderStorageBuffer(int size)
        {
            if (!GLExtensions.Features430)
                throw new PlatformNotSupportedException("Platform does not support Shader Storage Buffer Objects");
            ID = GL.GenBuffer();
            GL.BindBuffer(GL.GL_SHADER_STORAGE_BUFFER, ID);
            GL.BufferData(GL.GL_SHADER_STORAGE_BUFFER, new IntPtr(size), IntPtr.Zero, GL.GL_DYNAMIC_DRAW);
            GL.BindBuffer(GL.GL_SHADER_STORAGE_BUFFER, 0);
        }
        public SSBOHandle Map(bool read = false)
        {
            GL.BindBuffer(GL.GL_SHADER_STORAGE_BUFFER, ID);
            var ptr = GL.MapBuffer(GL.GL_SHADER_STORAGE_BUFFER, (uint)(read ? GL.GL_READ_WRITE : GL.GL_WRITE_ONLY));
            return new SSBOHandle(this, ptr);
        }
        public void Unmap()
        {
            GL.UnmapBuffer(GL.GL_SHADER_STORAGE_BUFFER);
            GL.BindBuffer(GL.GL_SHADER_STORAGE_BUFFER, 0);
        }

        public void BindIndex(uint index)
        {
            GL.BindBufferBase(GL.GL_SHADER_STORAGE_BUFFER, index, ID);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(ID);
        }
    }
}
