using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
namespace LibreLancer.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionNormal : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;

        public VertexPositionNormal(BinaryReader reader)
            : this()
        {
            this.Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            this.Normal = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
		public void SetVertexPointers(int offset)
        {
            GL.EnableVertexAttribArray(VertexSlots.Position);
            GL.EnableVertexAttribArray(VertexSlots.Normal);
            GL.VertexAttribPointer(VertexSlots.Position, 3, VertexAttribPointerType.Float, false, VertexSize(), offset + 0);
            GL.VertexAttribPointer(VertexSlots.Normal, 3, VertexAttribPointerType.Float, false, VertexSize(), offset + sizeof(float) * 3);
        }
        public int VertexSize()
        {
            return sizeof(float) * 3 + sizeof(float) * 3;
        }
    }
}
