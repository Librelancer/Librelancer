using System;

namespace LibreLancer.Graphics.Backends;

public interface ITexture : IDisposable
{
    SurfaceFormat Format { get; }
    int EstimatedTextureMemory { get; }
    int LevelCount { get; }
    bool IsDisposed { get; }
    void SetFiltering(TextureFiltering filtering);
    void SetWrapModeS(WrapMode mode);
    void SetWrapModeT(WrapMode mode);
    void BindTo(int unit);
}
