using System;

namespace LibreLancer.Graphics.Backends;

internal interface IDepthBuffer : IDisposable
{
    bool HasStencil { get; }
}
