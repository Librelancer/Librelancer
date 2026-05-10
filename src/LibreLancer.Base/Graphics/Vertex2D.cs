using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Graphics.Vertices;

namespace LibreLancer.Graphics;

[StructLayout(LayoutKind.Sequential)]
struct Vertex2D : IVertexType
{
    public const int Size = 5 * sizeof(float);

    public Vector2 Position;
    public Vector2 TexCoord;
    public VertexDiffuse Color;

    public Vertex2D(Vector2 position, Vector2 texcoord, Color4 color)
    {
        Position = position;
        TexCoord = texcoord;
        Color = (VertexDiffuse)color;
    }

    public VertexDeclaration GetVertexDeclaration()
    {
        return new VertexDeclaration (
            Size,
            new VertexElement (VertexSlots.Position, 2, VertexElementType.Float, false, 0),
            new VertexElement (VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 2),
            new VertexElement (VertexSlots.Color, 4, VertexElementType.UnsignedByte, true, sizeof(float) * 4)
        );
    }
}
