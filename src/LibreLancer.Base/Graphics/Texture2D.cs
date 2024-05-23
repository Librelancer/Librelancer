// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
using LibreLancer.Graphics.Backends;
using LibreLancer.Graphics.Backends.OpenGL;

namespace LibreLancer.Graphics
{
    public class Texture2D : Texture
    {
        internal ITexture2D Backing;
        public int Width => Backing.Width;
        public int Height => Backing.Height;
        public bool WithAlpha { get; set; }
        public bool Dxt1 => Backing.Dxt1;

        public Texture2D(RenderContext context, int width, int height, bool hasMipMaps, SurfaceFormat format)
        {
            Backing = context.Backend.CreateTexture2D(width, height, hasMipMaps, format);
            SetBacking(Backing);
        }

        public Texture2D(RenderContext context, int width, int height) : this(context, width, height, false, SurfaceFormat.Bgra8)
        {
        }

        protected internal Texture2D()
        {

        }

        protected internal void SetBacking2D(ITexture2D backing)
        {
            Backing = backing;
            SetBacking(Backing);
        }

        public void SetFiltering(TextureFiltering filtering) =>
            Backing.SetFiltering(filtering);


        public void GetData<T>(int level, Rectangle? rect, T[] data, int start, int count) where T : struct
            => Backing.GetData(level, rect, data, start, count);

        public void GetData<T>(T[] data) where T : struct =>
            Backing.GetData(data);

        public unsafe void SetData<T>(int level, Rectangle? rect, T[] data, int start, int count) where T : unmanaged =>
            Backing.SetData(level, rect, data, start, count);

        public void SetWrapModeS(WrapMode mode) =>
            Backing.SetWrapModeS(mode);

        public void SetWrapModeT(WrapMode mode) =>
            Backing.SetWrapModeT(mode);

        internal void SetData(int level, Rectangle rect, IntPtr data) =>
            Backing.SetData(level, rect, data);

        public void SetData<T>(T[] data) where T : unmanaged =>
            Backing.SetData(data);
    }
}

