// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics.Backends;

namespace LibreLancer.Graphics;

public class StorageBuffer : IDisposable
{
    private IStorageBuffer impl;
    public StorageBuffer(RenderContext renderContext, int size, int stride)
    {
        impl = renderContext.Backend.CreateStorageBuffer(size, stride);
    }

    public int GetAlignedIndex(int input) => impl.GetAlignedIndex(input);


    public void BindTo(int binding, int start = 0, int count = 0)
        => impl.BindTo(binding, start, count);

    public ref T Data<T>(int i) where T : unmanaged
        => ref impl.Data<T>(i);

    public IntPtr BeginStreaming() => impl.BeginStreaming();

    //Count is for if emulation is required
    public void EndStreaming(int count) => impl.EndStreaming(count);

    public void Dispose() => impl.Dispose();
}
