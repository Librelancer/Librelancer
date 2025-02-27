using System;
using System.Numerics;

namespace LibreLancer.Graphics.Backends;

interface IShader
{
    void SetUniformBlock<T>(int index, ref T data) where T : unmanaged;
    bool HasUniformBlock(int index);
    ref ulong UniformBlockTag(int index);
}
