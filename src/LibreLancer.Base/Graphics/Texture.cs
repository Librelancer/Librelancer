// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Threading;
using LibreLancer.Graphics.Backends;
using LibreLancer.Graphics.Backends.OpenGL;

namespace LibreLancer.Graphics;

public abstract class Texture : IDisposable
{
    /// <summary>
    /// Unique identifier for the texture. Not used for GL
    /// </summary>
    public uint ID { get; private set; }
    private static uint _unique = 0;

    internal ITexture Backing = null!;

    protected internal Texture()
    {
        ID = Interlocked.Increment(ref _unique);
    }

    protected void SetBacking(ITexture implementation) => Backing = implementation;

    public SurfaceFormat Format => Backing.Format;

    public int EstimatedTextureMemory => Backing.EstimatedTextureMemory;
    public int LevelCount => Backing.LevelCount;
    public bool IsDisposed => Backing.IsDisposed;

    public virtual void Dispose() => Backing.Dispose();
}
