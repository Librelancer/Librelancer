// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
namespace LibreLancer
{
    public class UniformBuffer : IDisposable
    {
        private Type storageType;
        private uint ID;
        private int stride;
        private int size;
        public UniformBuffer(int size, int stride, Type type)
        {
            this.stride = stride;
            this.size = size;
            storageType = type;
            ID = GL.GenBuffer();
            GLBind.UniformBuffer(ID);
            GL.BufferData(GL.GL_UNIFORM_BUFFER, new IntPtr(size * stride), IntPtr.Zero, GL.GL_STREAM_DRAW);
        }
        
        public void SetData<T>(T[] array, int length = -1) where T : struct
        {
            if (typeof(T) != storageType) throw new InvalidOperationException();
            var len = length < 0 ? array.Length : length;
            GLBind.UniformBuffer(ID);
            var handle = GCHandle.Alloc (array, GCHandleType.Pinned);
            GL.BufferSubData (GL.GL_UNIFORM_BUFFER, IntPtr.Zero, (IntPtr)(len * stride), handle.AddrOfPinnedObject());
            handle.Free();
        }

        public void BindTo(int binding, int start = 0, int count = 0)
        {
            var startPtr = (IntPtr) (start * stride);
            var length = (IntPtr) ((count <= 0 ? size : count)  * stride);
            GL.BindBufferRange(GL.GL_UNIFORM_BUFFER, (uint) binding, ID, startPtr, length);
        }

        public void Dispose()
        {
            GL.DeleteBuffers(1, ref ID);
        }
    }
}