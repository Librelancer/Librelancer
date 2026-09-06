using System;

namespace LibreLancer.Graphics.Backends;

public interface IRenderTarget : IDisposable
{
    bool HasStencil { get; }
}
