using System;

namespace LibreLancer.Graphics.Backends;

internal interface IStorageBuffer : IDisposable
{
    int GetAlignedIndex(int input);
    void BindTo(int binding, int start = 0, int count = 0);
    ref T Data<T>(int i) where T : unmanaged;
    IntPtr BeginStreaming();
    void EndStreaming(int count);
}
