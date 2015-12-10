using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
namespace LibreLancer.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionColor : IVertexType
    {
        public Vector3 Position;
        public Color4 Color;

        public VertexPositionColor(Vector3 pos, Color4 color)
        {
            Position = pos;
            Color = color;
        }
		public void SetVertexPointers(int offset)
        {
            GL.EnableVertexAttribArray(VertexSlots.Position);
            GL.EnableVertexAttribArray(VertexSlots.Color);
            GL.VertexAttribPointer(VertexSlots.Position, 3, VertexAttribPointerType.Float, false, VertexSize(), offset);
            GL.VertexAttribPointer(VertexSlots.Color, 4, VertexAttribPointerType.Float, false, VertexSize(), offset + sizeof(float) * 3);
        }
        public int VertexSize()
        {
            return sizeof(float) * 3 + sizeof(float) * 4;
        }
    }
}