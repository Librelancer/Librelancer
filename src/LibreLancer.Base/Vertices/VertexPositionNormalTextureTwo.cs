// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Runtime.InteropServices;

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

		public VertexDeclaration GetVertexDeclaration() 
		{
			return new VertexDeclaration (
				sizeof(float) * 3 + sizeof(float) * 3 + (sizeof(float) * 2) * 2,
				new VertexElement (VertexSlots.Position, 3, VertexElementType.Float, false, 0),
				new VertexElement (VertexSlots.Normal, 3, VertexElementType.Float, false, sizeof(float) * 3),
				new VertexElement (VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 6),
				new VertexElement (VertexSlots.Texture2, 2, VertexElementType.Float, false, sizeof(float) * 8)
			);
		}
    }
}