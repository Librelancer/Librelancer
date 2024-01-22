// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics.Backends;

namespace LibreLancer.Graphics
{
    public class UniformBuffer : IDisposable
    {
        private Type storageType;
        private uint ID;
        private int stride;
        private int size;
        int gAlignment;
        private bool streaming;

        private IUniformBuffer impl;
        public UniformBuffer(RenderContext renderContext, int size, int stride, Type type, bool streaming = false)
        {
            impl = renderContext.Backend.CreateUniformBuffer(size, stride, type, streaming);
        }

        public int GetAlignedIndex(int input) => impl.GetAlignedIndex(input);

        public void SetData<T>(T[] array, int start = 0, int length = -1) where T : unmanaged
            => impl.SetData(array, start, length);

        public unsafe void SetData<T>(ref T item, int index = 0) where T : unmanaged
            => impl.SetData(ref item, index);

        public void BindTo(int binding, int start = 0, int count = 0)
            => impl.BindTo(binding, start, count);

        public unsafe ref T Data<T>(int i) where T : unmanaged
            => ref impl.Data<T>(i);

        public IntPtr BeginStreaming() => impl.BeginStreaming();

        //Count is for if emulation is required
        public void EndStreaming(int count) => impl.EndStreaming(count);

        public void Dispose() => impl.Dispose();
    }
}
