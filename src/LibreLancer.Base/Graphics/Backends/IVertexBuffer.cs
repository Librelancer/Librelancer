using System;
using LibreLancer.Graphics.Vertices;

namespace LibreLancer.Graphics.Backends;

interface IVertexBuffer : IDisposable
{
    IVertexType VertexType { get; }
    int VertexCount { get; }
    public void SetData<T>(ReadOnlySpan<T> data, int offset = 0) where T : unmanaged;
    void Expand(int newSize);
    void Draw(PrimitiveTypes primitiveType, int baseVertex, int startIndex, int primitiveCount);
    unsafe void DrawImmediateElements(PrimitiveTypes primitiveTypes, int baseVertex, ReadOnlySpan<ushort> elements);
    IntPtr BeginStreaming();
    void EndStreaming(int count);
    void Draw(PrimitiveTypes primitiveType, int primitiveCount);
    void DrawNoApply(PrimitiveTypes primitiveType, int primitiveCount);
    void Draw(PrimitiveTypes primitiveType, int start, int primitiveCount);
    void SetElementBuffer(IElementBuffer elems);
    void UnsetElementBuffer();
}
