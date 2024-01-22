// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;

namespace LibreLancer.Graphics.Backends.OpenGL
{
    class GLUniformBuffer : IUniformBuffer
    {
        private Type storageType;
        private uint ID;
        private int stride;
        private int size;
        int gAlignment;
        private bool streaming;
        public GLUniformBuffer(int size, int stride, Type type, bool streaming = false)
        {
            if(stride % 16 != 0) throw new Exception("Must be aligned to minimum 16");
            this.stride = stride;
            this.size = size;
            storageType = type;
            ID = GL.GenBuffer();
            GLBind.UniformBuffer(ID);
            GL.BufferData(GL.GL_UNIFORM_BUFFER, new IntPtr(size * stride), IntPtr.Zero, GL.GL_STREAM_DRAW);
            GL.GetIntegerv(GL.GL_UNIFORM_BUFFER_OFFSET_ALIGNMENT, out int align);
            if(streaming)
                buffer = Marshal.AllocHGlobal(size * stride);
            this.streaming = streaming;
            var lval = stride % align;
            if (lval == 0) gAlignment = 0;
            else gAlignment = align;
            #if DEBUG
            if(align < 256 && 256 % align != 0)
                gAlignment = 256; //Set larger alignment on debug for testing
            #endif
        }
        public int GetAlignedIndex(int input)
        {
            if (gAlignment == 0) return input;
            int offset = input * stride;
            var aOffset = (offset + (gAlignment - 1)) & ~(gAlignment - 1);
            return aOffset / stride;
        }
        public void SetData<T>(T[] array, int start = 0, int length = -1) where T : unmanaged
        {
            if (typeof(T) != storageType) throw new InvalidOperationException();
            var len = length < 0 ? array.Length : length;
            GLBind.UniformBuffer(ID);
            var handle = GCHandle.Alloc (array, GCHandleType.Pinned);
            GL.BufferSubData (GL.GL_UNIFORM_BUFFER, (IntPtr)(start * stride), (IntPtr)(len * stride), handle.AddrOfPinnedObject());
            handle.Free();
        }

        public unsafe void SetData<T>(ref T item, int index = 0) where T : unmanaged
        {
            if (typeof(T) != storageType) throw new InvalidOperationException();
            GLBind.UniformBuffer(ID);
            fixed (T* p = &item) {
                GL.BufferSubData (GL.GL_UNIFORM_BUFFER, (IntPtr)(index * stride), (IntPtr)( stride), (IntPtr)p);
            }
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

        public unsafe ref T Data<T>(int i) where T : unmanaged
        {
            if (i >= size) throw new IndexOutOfRangeException();
            return ref ((T*)buffer)[i];
        }

        private IntPtr buffer;
        public IntPtr BeginStreaming()
        {
            if (!streaming) throw new InvalidOperationException("not streaming buffer");
            return buffer;
        }

        //Count is for if emulation is required
        public void EndStreaming(int count)
        {
            if (!streaming) throw new InvalidOperationException("not streaming buffer");
            if (count == 0) return;
            GLBind.UniformBuffer(ID);
            GL.BufferData(GL.GL_UNIFORM_BUFFER, (IntPtr)(size * stride), IntPtr.Zero, GL.GL_STREAM_DRAW);
            GL.BufferSubData(GL.GL_UNIFORM_BUFFER, IntPtr.Zero, (IntPtr) (count * stride), buffer);
        }

        public void Dispose()
        {
            GL.DeleteBuffers(1, ref ID);
            if(streaming)
                Marshal.FreeHGlobal(buffer);
        }
    }
}
