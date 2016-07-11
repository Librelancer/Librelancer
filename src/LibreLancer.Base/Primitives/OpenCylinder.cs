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
 * The Original Code is RenderTools code (http://flapi.sourceforge.net/).
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011, 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using LibreLancer.Vertices;

namespace LibreLancer.Primitives
{
    public class OpenCylinder : IDisposable
    {
        public VertexBuffer VertexBuffer { get; private set; }
		ElementBuffer elemBuffer;
		int primCount;
		VertexPositionNormalTexture[] vertices;
		ushort[] indices;
		ushort[] indices_temp;
		int[] indices_sort;
        public OpenCylinder(int slices)
        {
            vertices = new VertexPositionNormalTexture[(slices + 2) * 2];
            indices = new ushort[slices * 6];
			indices_temp = new ushort[slices * 6];
			indices_sort = new int[slices * 2];
			primCount = slices * 2;
			float height = 0.5f, radius = 1;
			int iptr = 0;
			int vptr = 0;
			float texAdvance = 1f / slices;
			for (int i = 0; i < slices; i++)
			{
				var normal = GetCircleVector(i, slices);
				//generate vertices
				vertices[vptr++] = new VertexPositionNormalTexture(
					normal * radius + VectorMath.Up * height,
					normal,
					new Vector2(i, 1)
				);
				vertices[vptr++] = new VertexPositionNormalTexture(
					normal * radius + VectorMath.Down * height,
					normal,
					new Vector2(i, 0)
				);
				//generate indicess
				indices[iptr++] = (ushort)((i * 2 + 2) % ((slices + 1) * 2));
				indices[iptr++] = (ushort)(i * 2 + 1);
				indices[iptr++] = (ushort)(i * 2);

				indices[iptr++] = (ushort)((i * 2 + 2) % ((slices + 1) * 2));
				indices[iptr++] = (ushort)((i * 2 + 3) % ((slices + 1) * 2));
				indices[iptr++] = (ushort)(i * 2 + 1);
			}
			//last vertex
			var last = GetCircleVector(slices, slices);

			vertices[vptr++] = new VertexPositionNormalTexture(
				last * radius + VectorMath.Up * height,
				last,
				new Vector2(slices, 1)
			);
			vertices[vptr++] = new VertexPositionNormalTexture(
				last * radius + VectorMath.Down * height,
				last,
				new Vector2(slices, 0)
			);
			//upload
			VertexBuffer = new VertexBuffer(typeof(VertexPositionNormalTexture), vertices.Length);
			VertexBuffer.SetData(vertices);
			elemBuffer = new ElementBuffer(indices.Length);
			elemBuffer.SetData(indices);
			VertexBuffer.SetElementBuffer(elemBuffer);
        }
		public Vector3 GetSidePosition(int side)
		{
			var v0 = vertices[indices[side * 6]].Position;
			var v1 = vertices[indices[side * 6 + 1]].Position;
			var v2 = vertices[indices[side * 6 + 2]].Position;
			var v3 = vertices[indices[side * 6 + 3]].Position;
			var v4 = vertices[indices[side * 6 + 4]].Position;
			var v5 = vertices[indices[side * 6 + 5]].Position;
			return (v0 + v1 + v2 + v3 + v4 + v5) / 6f;
		}
		public int PrimitiveCount
		{
			get
			{
				return primCount;
			}
		}
		static Vector3 GetCircleVector(int i, int slices)
		{
			float angle = i * MathHelper.TwoPi / slices;
			float dx = (float)Math.Cos(angle);
			float dz = (float)Math.Sin(angle);
			return new Vector3(dx, 0, dz);
		}
        public void Dispose()
        {
            VertexBuffer.Dispose();
			elemBuffer.Dispose();
        }
    }
}
