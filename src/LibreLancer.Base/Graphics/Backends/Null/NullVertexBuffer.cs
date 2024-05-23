using System;
using System.Runtime.InteropServices;
using LibreLancer.Graphics.Vertices;

namespace LibreLancer.Graphics.Backends.Null;

class NullVertexBuffer : IVertexBuffer
{
    private IntPtr buffer;
    private bool isStream = false;
    public void Dispose()
    {
        if (buffer != IntPtr.Zero)
            Marshal.FreeHGlobal(buffer);
    }

    public NullVertexBuffer(Type type, int length, bool isStream = false)
    {
        try
        {
            VertexType = (IVertexType)Activator.CreateInstance (type);
            decl = VertexType.GetVertexDeclaration();
        }
        catch (Exception)
        {
            throw new Exception(string.Format("{0} is not a valid IVertexType", type.FullName));
        }
        if(isStream)
            buffer = Marshal.AllocHGlobal(length * decl.Stride);
        VertexCount = length;
        this.isStream = isStream;
    }

    public NullVertexBuffer(IVertexType type, int length, bool isStream = false)
    {
        VertexType = type;
        decl = VertexType.GetVertexDeclaration();
        if(isStream)
            buffer = Marshal.AllocHGlobal(length * decl.Stride);
        VertexCount = length;
        this.isStream = isStream;
    }


    private VertexDeclaration decl;

    public IVertexType VertexType { get; set;  }
    public int VertexCount { get; set;  }

    public unsafe void SetData<T>(ReadOnlySpan<T> data, int offset = 0) where T : unmanaged
    {
    }

    public void Expand(int newSize)
    {
        VertexCount = newSize;
        if (isStream)
        {
            Marshal.FreeHGlobal(buffer);
            buffer = Marshal.AllocHGlobal(newSize * decl.Stride);
        }
    }

    public void Draw(PrimitiveTypes primitiveType, int baseVertex, int startIndex, int primitiveCount)
    {
    }

    public unsafe void DrawImmediateElements(PrimitiveTypes primitiveTypes, int baseVertex, ReadOnlySpan<ushort> elements)
    {
    }

    public IntPtr BeginStreaming() => buffer;

    public void EndStreaming(int count)
    {
    }

    public void Draw(PrimitiveTypes primitiveType, int primitiveCount)
    {
    }

    public void DrawNoApply(PrimitiveTypes primitiveType, int primitiveCount)
    {
    }

    public void Draw(PrimitiveTypes primitiveType, int start, int primitiveCount)
    {
    }

    public void SetElementBuffer(IElementBuffer elems)
    {
    }

    public void UnsetElementBuffer()
    {
    }
}
