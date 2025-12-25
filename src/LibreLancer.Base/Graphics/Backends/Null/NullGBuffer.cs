// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Graphics.Backends.Null;

/// <summary>
/// Null implementation of G-Buffer for headless/testing scenarios.
/// </summary>
class NullGBuffer : IGBuffer
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    private NullTexture2D positionTexture;
    private NullTexture2D normalTexture;
    private NullTexture2D albedoTexture;
    private NullTexture2D materialTexture;
    private NullTexture2D depthTexture;
    private bool disposed;

    public ITexture2D PositionTexture => positionTexture;
    public ITexture2D NormalTexture => normalTexture;
    public ITexture2D AlbedoTexture => albedoTexture;
    public ITexture2D MaterialTexture => materialTexture;
    public ITexture2D DepthTexture => depthTexture;

    public NullGBuffer(int width, int height)
    {
        Width = width;
        Height = height;

        // Create null textures for each G-Buffer component
        positionTexture = new NullTexture2D(width, height, false, SurfaceFormat.HalfVector4, 1);
        normalTexture = new NullTexture2D(width, height, false, SurfaceFormat.HalfVector4, 1);
        albedoTexture = new NullTexture2D(width, height, false, SurfaceFormat.Bgra8, 1);
        materialTexture = new NullTexture2D(width, height, false, SurfaceFormat.Bgra8, 1);
        depthTexture = new NullTexture2D(width, height, false, SurfaceFormat.Depth32F, 1);
    }

    public void BindForWriting()
    {
        // No-op for null backend
    }

    public void BindForReading(int startUnit = 0)
    {
        // No-op for null backend
    }

    public void Clear()
    {
        // No-op for null backend
    }

    public void Resize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public void Unbind()
    {
        // No-op for null backend
    }

    public void BindDepthForReading(int unit)
    {
        // No-op for null backend
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        // Dispose textures for consistency
        positionTexture?.Dispose();
        normalTexture?.Dispose();
        albedoTexture?.Dispose();
        materialTexture?.Dispose();
        depthTexture?.Dispose();
    }
}
