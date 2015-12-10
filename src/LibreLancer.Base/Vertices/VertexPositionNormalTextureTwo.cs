using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
namespace LibreLancer.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
	public struct VertexPositionNormalTextureTwo : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoordinate;
        public Vector2 TextureCoordinateTwo;

        public VertexPositionNormalTextureTwo(BinaryReader reader)
            : this()
        {
            this.Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            this.Normal = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
			this.TextureCoordinate = new Vector2(reader.ReadSingle(), 1 - reader.ReadSingle());
			this.TextureCoordinateTwo = new Vector2(reader.ReadSingle(), 1 - reader.ReadSingle());
        }
			
		public void SetVertexPointers (int offset)
		{
			GL.EnableVertexAttribArray(VertexSlots.Position);
			GL.EnableVertexAttribArray(VertexSlots.Normal);
			GL.EnableVertexAttribArray(VertexSlots.Texture1);
			GL.VertexAttribPointer(VertexSlots.Position, 3, VertexAttribPointerType.Float, false, VertexSize(), offset + 0);
			GL.VertexAttribPointer(VertexSlots.Normal, 3, VertexAttribPointerType.Float, false, VertexSize(), offset + sizeof(float) * 3);
			GL.VertexAttribPointer(VertexSlots.Texture1, 2, VertexAttribPointerType.Float, false, VertexSize(), offset + sizeof(float) * 6);
			GL.VertexAttribPointer (VertexSlots.Texture2, 2, VertexAttribPointerType.Float, false, VertexSize (), offset + sizeof(float) * 8);
		}

		public int VertexSize ()
		{
			return sizeof(float) * 3 + sizeof(float) * 3 + (sizeof(float) * 2) * 2;
		}
    }
}