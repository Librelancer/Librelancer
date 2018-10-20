// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
using LibreLancer.Vertices;
namespace LibreLancer.Utf.Dfm
{
	[StructLayout(LayoutKind.Sequential)]
	public struct DfmVertex : IVertexType
	{
		public Vector3 Position;
		public Vector3 Normal;
		public Vector2 TextureCoordinate;
		public ushort BoneFirst;
		public ushort BoneCount;
		public DfmVertex(Vector3 pos, Vector3 normal, Vector2 texcoord, int boneFirst, int boneCount)
		{
			Position = pos;
			Normal = normal;
			TextureCoordinate = texcoord;
			BoneFirst = checked((ushort)boneFirst);
			BoneCount = checked((ushort)boneCount);
		}

		public VertexDeclaration GetVertexDeclaration()
		{
			return new VertexDeclaration(
				sizeof(float) * 3 + sizeof(float) * 3 + sizeof(float) * 2 + sizeof(ushort) * 2,
				new VertexElement(VertexSlots.Position, 3, VertexElementType.Float, false, 0),
				new VertexElement(VertexSlots.Normal, 3, VertexElementType.Float, false, sizeof(float) * 3),
				new VertexElement(VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 6),
				new VertexElement(VertexSlots.BoneFirst, 1, VertexElementType.UnsignedShort, false, sizeof(float) * 8),
				new VertexElement(VertexSlots.BoneCount, 1, VertexElementType.UnsignedShort, false, sizeof(float) * 8 + sizeof(ushort) * 1)
			);
		}
	}
}

