// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics.Backends;

namespace LibreLancer.Graphics;

public class DepthBuffer : IDisposable
{
    internal IDepthBuffer Backing;
    public DepthBuffer(RenderContext context, int width, int height, bool stencil = false)
    {
        Backing = context.Backend.CreateDepthBuffer(width, height, stencil);
    }
    public void Dispose()
    {
        Backing.Dispose();
    }
}
