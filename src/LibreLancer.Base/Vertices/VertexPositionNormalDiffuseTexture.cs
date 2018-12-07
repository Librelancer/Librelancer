// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;

namespace LibreLancer.Vertices
{
	[StructLayout(LayoutKind.Sequential)]
	public struct VertexPositionNormalDiffuseTexture : IVertexType
	{
		public Vector3 Position;
		public Vector3 Normal;
		public uint Diffuse;
		public Vector2 TextureCoordinate;
		public VertexPositionNormalDiffuseTexture(Vector3 pos, Vector3 normal, uint diffuse, Vector2 texcoord)
		{
			Position = pos;
			Normal = normal;
			Diffuse = diffuse;
			TextureCoordinate = texcoord;
		}

		public VertexDeclaration GetVertexDeclaration()
		{
			return new VertexDeclaration(
				sizeof(float) * 3 + + sizeof(float) * 3 + sizeof(int) + sizeof(float) * 2,
				new VertexElement(VertexSlots.Position, 3, VertexElementType.Float, false, 0),
				new VertexElement(VertexSlots.Normal, 3, VertexElementType.Float, false, sizeof(float) * 3),
				new VertexElement(VertexSlots.Color, 4, VertexElementType.UnsignedByte, true, sizeof(float) * 6),
				new VertexElement(VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 7)
			);
		}

		public static bool operator ==(VertexPositionNormalDiffuseTexture left, VertexPositionNormalDiffuseTexture right)
		{
			return left.Position == right.Position &&
					   left.Normal == right.Normal &&
					   left.Diffuse == right.Diffuse &&
					   left.TextureCoordinate == right.TextureCoordinate;
		}

		public static bool operator !=(VertexPositionNormalDiffuseTexture left, VertexPositionNormalDiffuseTexture right)
		{
			return left.Position != right.Position ||
					   left.Normal != right.Normal ||
					   left.Diffuse != right.Diffuse ||
					   left.TextureCoordinate != right.TextureCoordinate;
		}
	}
}

