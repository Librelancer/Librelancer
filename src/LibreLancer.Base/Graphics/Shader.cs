// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Numerics;
using LibreLancer.Graphics.Backends;

namespace LibreLancer.Graphics
{
    public class Shader
    {
        internal IShader Backing;
        public Shader(RenderContext context, ReadOnlySpan<byte> program)
        {
            Backing = context.Backend.CreateShader(program);
        }

        private Shader()
        {
        }

        public bool HasUniformBlock(int index) => Backing.HasUniformBlock(index);

        public ref ulong UniformBlockTag(int index) => ref Backing.UniformBlockTag(index);

        public void SetUniformBlock<T>(int index, ref T data) where T : unmanaged => Backing.SetUniformBlock(index, ref data);
    }
}
