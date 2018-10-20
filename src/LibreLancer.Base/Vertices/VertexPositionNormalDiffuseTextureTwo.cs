// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LibreLancer.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionNormalDiffuseTextureTwo : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color4 Diffuse;
        public Vector2 TextureCoordinate;
        public Vector2 TextureCoordinateTwo;

        public VertexPositionNormalDiffuseTextureTwo(BinaryReader reader)
            : this()
        {
            this.Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            this.Normal = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
			Diffuse = new Color4(reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f);
			this.TextureCoordinate = new Vector2(reader.ReadSingle(), 1 - reader.ReadSingle());
			this.TextureCoordinateTwo = new Vector2(reader.ReadSingle(), 1 - reader.ReadSingle());
        }
        public VertexPositionNormalDiffuseTextureTwo(Vector3 pos, Vector3 normal, Color4 diffuse, Vector2 tex1, Vector2 tex2)
        {
            Position = pos;
            Normal = normal;
            Diffuse = diffuse;
            TextureCoordinate = tex1;
            TextureCoordinateTwo = tex2;
        }
		public VertexDeclaration GetVertexDeclaration()
		{
			return new VertexDeclaration(
				sizeof(float) * 3 + + sizeof(float) * 3 + sizeof(float) * 4 + sizeof(float) * 2 + sizeof(float) * 2,
				new VertexElement(VertexSlots.Position, 3, VertexElementType.Float, false, 0),
				new VertexElement(VertexSlots.Normal, 3, VertexElementType.Float, false, sizeof(float) * 3),
				new VertexElement(VertexSlots.Color, 4, VertexElementType.Float, false, sizeof(float) * 6),
				new VertexElement(VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 10),
				new VertexElement(VertexSlots.Texture2, 2, VertexElementType.Float, false, sizeof(float) * 12)
			);
		}
    }
}