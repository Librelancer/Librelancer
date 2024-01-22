using System;

namespace LibreLancer.Graphics.Backends;

interface IElementBuffer : IDisposable
{
    int IndexCount { get; }
    void SetData(short[] data);
    void SetData(ushort[] data);
    void SetData(ushort[] data, int count, int start = 0);
    void Expand(int newSize);
}
