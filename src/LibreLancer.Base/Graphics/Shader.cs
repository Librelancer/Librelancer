// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Numerics;
using System.Threading;
using LibreLancer.Graphics.Backends;

namespace LibreLancer.Graphics;

public class Shader
{
    private static int _shaderCount = 0;

    public static int TotalShaders => _shaderCount;

    internal readonly IShader Backing = null!;
    public Shader(RenderContext context, ReadOnlySpan<byte> program)
    {
        Backing = context.Backend.CreateShader(program);
        Interlocked.Increment(ref _shaderCount);
    }

    private Shader()
    {
    }

    public bool HasUniformBlock(int index) => Backing.HasUniformBlock(index);

    public ref ulong UniformBlockTag(int index)
    {
        return ref Backing.UniformBlockTag(index);
    }

    public void SetUniformBlock<T>(int index, ref T data, bool forceUpdate = false, int forceSize = -1) where T : unmanaged => Backing.SetUniformBlock(index, ref data, forceUpdate, forceSize);
}
