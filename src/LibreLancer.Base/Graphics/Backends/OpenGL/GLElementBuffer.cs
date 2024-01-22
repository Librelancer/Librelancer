// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;

namespace LibreLancer.Graphics.Backends.OpenGL
{
    unsafe class GLElementBuffer : IElementBuffer
    {
        public int IndexCount { get; private set; }
        public uint Handle;
        internal List<GLVertexBuffer> VertexBuffers = new List<GLVertexBuffer>();
        bool isDynamic;

        private GLRenderContext context;

        public GLElementBuffer(GLRenderContext context, int count, bool isDynamic = false)
        {
            this.context = context;
            this.isDynamic = isDynamic;
            IndexCount = count;
            Handle = GL.GenBuffer();
            GLBind.VertexArray(context.NullVAO);
            GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, Handle);
            GL.BufferData(GL.GL_ELEMENT_ARRAY_BUFFER, new IntPtr(count * 2), IntPtr.Zero,
                isDynamic ? GL.GL_DYNAMIC_DRAW : GL.GL_STATIC_DRAW);
        }

        private int maxSet;

        public void SetData(short[] data)
        {
            GLBind.VertexArray(context.NullVAO);
            GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, Handle);
            fixed (short* ptr = data)
            {
                maxSet = data.Length * 2;
                GL.BufferData(GL.GL_ELEMENT_ARRAY_BUFFER, new IntPtr(data.Length * 2), (IntPtr)ptr,
                    isDynamic ? GL.GL_DYNAMIC_DRAW : GL.GL_STATIC_DRAW);
            }
        }

        public void SetData(ushort[] data)
        {
            maxSet = Math.Max(maxSet, data.Length * 2);
            SetData(data, data.Length);
        }

        public void SetData(ushort[] data, int count, int start = 0)
        {
            GLBind.VertexArray(context.NullVAO);
            GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, Handle);
            fixed (ushort* ptr = data)
            {
                maxSet = Math.Max(maxSet, (start + count) * 2);
                GL.BufferSubData(GL.GL_ELEMENT_ARRAY_BUFFER, new IntPtr(start * 2), new IntPtr(count * 2), (IntPtr)ptr);
            }
        }

        public void Expand(int newSize)
        {
            if (newSize < IndexCount)
                throw new InvalidOperationException();
            var newHandle = GL.GenBuffer();
            GL.BindBuffer(GL.GL_COPY_READ_BUFFER, Handle);
            GL.BindBuffer(GL.GL_COPY_WRITE_BUFFER, newHandle);
            GL.BufferData(GL.GL_COPY_WRITE_BUFFER, new IntPtr(newSize * 2), IntPtr.Zero,
                isDynamic ? GL.GL_DYNAMIC_DRAW : GL.GL_STATIC_DRAW);
            GL.CopyBufferSubData(GL.GL_COPY_READ_BUFFER, GL.GL_COPY_WRITE_BUFFER, IntPtr.Zero, IntPtr.Zero,
                (IntPtr)maxSet);
            GL.DeleteBuffer(Handle);
            Handle = newHandle;
            IndexCount = newSize;
            foreach (var vbo in VertexBuffers)
                vbo.RefreshElementBuffer();
        }

        public void Dispose()
        {
            GL.DeleteBuffer(Handle);
        }
    }
}
