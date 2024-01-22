// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics.Backends;

namespace LibreLancer.Graphics
{
    public abstract class RenderTarget : IDisposable
    {
        internal IRenderTarget Target;
        public abstract void Dispose();
    }
}
