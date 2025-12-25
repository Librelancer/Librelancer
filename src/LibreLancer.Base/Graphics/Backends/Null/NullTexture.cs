// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Graphics.Backends.Null;

internal class NullTexture(SurfaceFormat format, int levelCount, int estimatedTextureMemory)
    : ITexture
{
    public SurfaceFormat Format { get; protected set; } = format;

    public int EstimatedTextureMemory { get; protected set; } = estimatedTextureMemory;

    public int LevelCount
    {
        get;
        protected set;
    } = levelCount;

    public bool IsDisposed { get; private set; } = false;

    public void BindTo(int unit)
    {
    }


    public virtual void Dispose()
    {
        IsDisposed = true;
    }
}
