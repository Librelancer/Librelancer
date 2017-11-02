/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
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

