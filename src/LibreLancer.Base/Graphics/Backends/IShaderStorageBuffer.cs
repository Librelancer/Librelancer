using System;

namespace LibreLancer.Graphics.Backends;

interface IShaderStorageBuffer : IDisposable
{
    int Size { get; }
    SSBOHandle Map(bool read = false);
    void Unmap();
    void BindIndex(uint index);
}
