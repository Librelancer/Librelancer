// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;

namespace LibreLancer.Vertices
{
	[StructLayout(LayoutKind.Sequential)]
	public struct VertexPositionNormalColorTexture : IVertexType
	{
		public Vector3 Position;
		public Vector3 Normal;
		public Color4 Color;
		public Vector2 TextureCoordinate;
		public VertexPositionNormalColorTexture(Vector3 pos, Vector3 normal, Color4 color, Vector2 texcoord)
		{
			Position = pos;
			Normal = normal;
			Color = color;
			TextureCoordinate = texcoord;
		}

		public VertexDeclaration GetVertexDeclaration()
		{
			return new VertexDeclaration(
				sizeof(float) * 3 + + sizeof(float) * 3 + sizeof(float) * 4 + sizeof(float) * 2,
				new VertexElement(VertexSlots.Position, 3, VertexElementType.Float, false, 0),
				new VertexElement(VertexSlots.Normal, 3, VertexElementType.Float, false, sizeof(float) * 3),
				new VertexElement(VertexSlots.Color, 4, VertexElementType.Float, false, sizeof(float) * 6),
				new VertexElement(VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 10)
			);
		}

		public static bool operator ==(VertexPositionNormalColorTexture left, VertexPositionNormalColorTexture right)
		{
			return left.Position == right.Position &&
					   left.Normal == right.Normal &&
					   left.Color == right.Color &&
					   left.TextureCoordinate == right.TextureCoordinate;
		}

		public static bool operator !=(VertexPositionNormalColorTexture left, VertexPositionNormalColorTexture right)
		{
			return left.Position != right.Position ||
					   left.Normal != right.Normal ||
					   left.Color != right.Color ||
					   left.TextureCoordinate != right.TextureCoordinate;
		}
	}
}

