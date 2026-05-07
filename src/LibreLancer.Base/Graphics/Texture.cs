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

    private ITexture backing = null!;

    protected internal Texture()
    {
        ID = Interlocked.Increment(ref _unique);
    }

    protected void SetBacking(ITexture implementation) => backing = implementation;

    public SurfaceFormat Format => backing.Format;

    public int EstimatedTextureMemory => backing.EstimatedTextureMemory;
    public int LevelCount => backing.LevelCount;
    public bool IsDisposed => backing.IsDisposed;
    public void BindTo(int unit) => backing.BindTo(unit);

    public void SetFiltering(TextureFiltering filtering) =>
        backing.SetFiltering(filtering);

    public void SetWrapModeS(WrapMode mode) =>
        backing.SetWrapModeS(mode);

    public void SetWrapModeT(WrapMode mode) =>
        backing.SetWrapModeT(mode);

    public virtual void Dispose() => backing.Dispose();
}
