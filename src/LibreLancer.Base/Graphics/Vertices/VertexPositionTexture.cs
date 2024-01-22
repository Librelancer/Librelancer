// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using System.Runtime.InteropServices;

namespace LibreLancer.Graphics.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionTexture : IVertexType
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public VertexPositionTexture(Vector3 pos, Vector2 texcoord)
        {
            Position = pos;
            TextureCoordinate = texcoord;
        }


		public VertexDeclaration GetVertexDeclaration()
		{
			return new VertexDeclaration (
				sizeof(float) * 3 + sizeof(float) * 2,
				new VertexElement (VertexSlots.Position, 3, VertexElementType.Float, false, 0),
				new VertexElement (VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 3)
			);
		}
    }
}
