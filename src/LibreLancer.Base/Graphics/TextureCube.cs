// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
using LibreLancer.Graphics.Backends;
using LibreLancer.Graphics.Backends.OpenGL;

namespace LibreLancer.Graphics
{
    public sealed class TextureCube : Texture
    {
        private ITextureCube impl;
        public int Size => impl.Size;

        public TextureCube (RenderContext context, int size, bool mipMap, SurfaceFormat format)
        {
            impl = context.Backend.CreateTextureCube(size, mipMap, format);
            SetBacking(impl);
        }

        private int maxLevel = 0;
        private int currentLevels = 0;

        public void SetData<T>(CubeMapFace face, int level, Rectangle? rect, T[] data, int start, int count)
            where T : unmanaged
            => impl.SetData(face, level, rect, data, start, count);

        public void SetData<T>(CubeMapFace face, T[] data) where T : unmanaged
            => impl.SetData(face, data);

        public void SetFiltering(TextureFiltering filtering)
            => impl.SetFiltering(filtering);
    }
}
