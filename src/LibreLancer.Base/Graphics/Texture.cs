// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Threading;
using LibreLancer.Graphics.Backends;
using LibreLancer.Graphics.Backends.OpenGL;

namespace LibreLancer.Graphics
{
    public abstract class Texture : IDisposable
    {
        /// <summary>
        /// Unique identifier for the texture. Not used for GL
        /// </summary>
        public uint ID { get; private set; }
        private static uint _unique = 0;

        private ITexture impl;

        protected internal Texture()
        {
            ID = Interlocked.Increment(ref _unique);
        }

        protected void SetBacking(ITexture impl) => this.impl = impl;

        public SurfaceFormat Format => impl.Format;

        public int EstimatedTextureMemory => impl.EstimatedTextureMemory;
        public int LevelCount => impl.LevelCount;

        public bool IsDisposed => impl.IsDisposed;

        public void BindTo(int unit) => impl.BindTo(unit);

        public virtual void Dispose() => impl.Dispose();
    }
}

