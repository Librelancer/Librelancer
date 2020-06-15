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
        int gAlignment;
        public UniformBuffer(int size, int stride, Type type)
        {
            if(stride % 16 != 0) throw new Exception("Must be aligned to minimum 16");
            this.stride = stride;
            this.size = size;
            storageType = type;
            ID = GL.GenBuffer();
            GLBind.UniformBuffer(ID);
            GL.BufferData(GL.GL_UNIFORM_BUFFER, new IntPtr(size * stride), IntPtr.Zero, GL.GL_STREAM_DRAW);
            GL.GetIntegerv(GL.GL_UNIFORM_BUFFER_OFFSET_ALIGNMENT, out int align);
            var lval = stride % align;
            if (lval == 0) gAlignment = 0;
            else gAlignment = align;
            #if DEBUG
            if(align < 256 && 256 % align != 0)
                gAlignment = 256; //Set larger alignment on debug for testing
            #endif
            if(align < stride && lval != 0)
                throw new Exception("Platform has incompatible alignment");
        }
        public int GetAlignedIndex(int input)
        {
            if (gAlignment == 0) return input;
            int offset = input * stride;
            var aOffset = (offset + (gAlignment - 1)) & ~(gAlignment - 1);
            return aOffset / stride;
        }
        public void SetData<T>(T[] array, int start = 0, int length = -1) where T : struct
        {
            if (typeof(T) != storageType) throw new InvalidOperationException();
            var len = length < 0 ? array.Length : length;
            GLBind.UniformBuffer(ID);
            var handle = GCHandle.Alloc (array, GCHandleType.Pinned);
            GL.BufferSubData (GL.GL_UNIFORM_BUFFER, (IntPtr)(start * stride), (IntPtr)(len * stride), handle.AddrOfPinnedObject());
            handle.Free();
        }

        public void BindTo(int binding, int start = 0, int count = 0)
        {
            if (GetAlignedIndex(start) != start)
                throw new InvalidOperationException("Uniform buffer alignment error");
            var startPtr = (IntPtr) (start * stride);
            var length = (IntPtr) ((count <= 0 ? size : count)  * stride);
            if((long)startPtr + (long)length > (size* stride))
                throw new IndexOutOfRangeException();
            GL.BindBufferRange(GL.GL_UNIFORM_BUFFER, (uint) binding, ID, startPtr, length);
        }

        public void Dispose()
        {
            GL.DeleteBuffers(1, ref ID);
        }
    }
}