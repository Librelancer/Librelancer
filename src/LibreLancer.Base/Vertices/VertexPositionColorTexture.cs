using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
namespace LibreLancer.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionColorTexture : IVertexType
    {
        public Vector3 Position;
        public Color4 Color;
        public Vector2 TextureCoordinate;
        public VertexPositionColorTexture(Vector3 pos, Color4 color, Vector2 texcoord)
        {
            Position = pos;
            Color = color;
            TextureCoordinate = texcoord;
        }
		public void SetVertexPointers(int offset)
        {
            GL.EnableVertexAttribArray(VertexSlots.Position);
            GL.EnableVertexAttribArray(VertexSlots.Color);
            GL.EnableVertexAttribArray(VertexSlots.Texture1);
            GL.VertexAttribPointer(VertexSlots.Position, 3, VertexAttribPointerType.Float, false, VertexSize(), offset);
            GL.VertexAttribPointer(VertexSlots.Color, 4, VertexAttribPointerType.Float, false, VertexSize(), offset + sizeof(float) * 3);
            GL.VertexAttribPointer(VertexSlots.Texture1, 2, VertexAttribPointerType.Float, false, VertexSize(), offset + sizeof(float) * 7);
        }
        public int VertexSize()
        {
            return sizeof(float) * 3 + 
                sizeof(float) * 4 + 
                sizeof(float) * 2;
        }
    }
}

