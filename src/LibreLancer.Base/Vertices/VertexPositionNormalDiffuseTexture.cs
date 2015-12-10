using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
namespace LibreLancer.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionNormalDiffuseTexture
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color4 Diffuse;
        public Vector2 TextureCoordinate;

        public VertexPositionNormalDiffuseTexture(BinaryReader reader)
            : this()
        {
            this.Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            this.Normal = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            //this.Diffuse = reader.ReadUInt32();
			Diffuse = new Color4(reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f);
			this.TextureCoordinate = new Vector2(reader.ReadSingle(), 1 - reader.ReadSingle());
        }
    }
}