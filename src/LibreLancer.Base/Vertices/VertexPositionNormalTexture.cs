using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
namespace LibreLancer.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionNormalTexture : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoordinate;

        public VertexPositionNormalTexture(Vector3 pos, Vector3 normal, Vector2 texcoord)
        {
            Position = pos;
            Normal = normal;
            TextureCoordinate = texcoord;
        }

		public void SetVertexPointers(int offset)
        {
            GL.EnableVertexAttribArray(VertexSlots.Position);
            GL.EnableVertexAttribArray(VertexSlots.Normal);
            GL.EnableVertexAttribArray(VertexSlots.Texture1);
            GL.VertexAttribPointer(VertexSlots.Position, 3, VertexAttribPointerType.Float, false, VertexSize(), offset + 0);
            GL.VertexAttribPointer(VertexSlots.Normal, 3, VertexAttribPointerType.Float, false, VertexSize(), offset + sizeof(float) * 3);
            GL.VertexAttribPointer(VertexSlots.Texture1, 2, VertexAttribPointerType.Float, false, VertexSize(), offset + sizeof(float) * 6);
        }
        public int VertexSize()
        {
            return sizeof(float) * 3 + sizeof(float) * 3 + sizeof(float) * 2;
        }
    }
}

