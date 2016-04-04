using System;
using System.Collections.Generic;
using OpenTK;
using LibreLancer.Vertices;

namespace LibreLancer.Primitives
{
	public class QuadSphere
	{
		VertexBuffer vertexBuffer;
		ElementBuffer elementBuffer;

		int primitiveCount;
		int primitiveCountSide;

		Dictionary<CubeMapFace,int> offsets = new Dictionary<CubeMapFace, int> ();

		public IVertexType VertexType {
			get {
				return vertexBuffer.VertexType;
			}
		}
		public QuadSphere (int slices)
		{
			//6 sides of slices^2 quads
			var indices = new ushort[(slices * slices) * 6 * 6];
			var indicesPerSide = (slices * slices) * 6;
			primitiveCount = indices.Length / 3;
			primitiveCountSide = indicesPerSide / 3;
			offsets.Add (CubeMapFace.PositiveY, 0);
			offsets.Add (CubeMapFace.NegativeY, indicesPerSide);
			offsets.Add (CubeMapFace.NegativeZ, 2 * indicesPerSide);
			offsets.Add (CubeMapFace.PositiveZ, 3 * indicesPerSide);
			offsets.Add (CubeMapFace.NegativeX, 4 * indicesPerSide);
			offsets.Add (CubeMapFace.PositiveX, 5 * indicesPerSide);
			var vertices = new VertexPositionTexture[(slices * slices) * 4 * 6];
			int verticesPerSide = (slices * slices) * 4;
			//Setup Indices
			int currIndex = 0;
			int currVertex = 0;
			//Top - POSITIVE Y
			Indices (2, 1, 0, 2, 3, 1, indices, ref currVertex, ref currIndex, verticesPerSide);
			//Bottom - NEGATIVE Y
			Indices (2, 0, 1, 2, 1, 3, indices, ref currVertex, ref currIndex, verticesPerSide);
			//front - NEGATIVE Z
			Indices (2, 1, 0, 2, 3, 1, indices, ref currVertex, ref currIndex, verticesPerSide);
			//back - POSITIVE Z
			Indices (2, 0, 1, 1, 3, 2, indices, ref currVertex, ref currIndex, verticesPerSide);
			//left - NEGATIVE X
			Indices (2, 1, 0, 2, 3, 1, indices, ref currVertex, ref currIndex, verticesPerSide);
			//right - POSITIVE X
			Indices (1, 2, 0, 3, 2, 1, indices, ref currVertex, ref currIndex, verticesPerSide);
			//setup vertices
			//BOTTOM
			int vertexCount = 0;
			TopBottom (1, slices, vertices, ref vertexCount);
			//TOP
			TopBottom (-1, slices, vertices, ref vertexCount);
			//FRONT
			FrontBack (-1, slices, vertices, ref vertexCount);
			//BACK
			FrontBack (1, slices, vertices, ref vertexCount);
			//LEFT
			LeftRight (-1, slices, vertices, ref vertexCount);
			//RIGHT
			LeftRight (1, slices, vertices, ref vertexCount);
			//Transform to Sphere
			for (int i = 0; i < vertices.Length; i++) {
				float x = vertices [i].Position.X;
				float y = vertices [i].Position.Y;
				float z = vertices [i].Position.Z;
				vertices [i].Position = new Vector3 (
					(float)(x * Math.Sqrt(1.0 - (y*y/2.0) - (z*z/2.0) + (y*y*z*z/3.0))),
					(float)(y * Math.Sqrt(1.0 - (z*z/2.0) - (x*x/2.0) + (z*z*x*x/3.0))),
					(float)(z * Math.Sqrt(1.0 - (x*x/2.0) - (y*y/2.0) + (x*x*y*y/3.0)))
				);
			}
			//Upload
			vertexBuffer = new VertexBuffer (typeof(VertexPositionTexture), vertices.Length);
			vertexBuffer.SetData (vertices);
			elementBuffer = new ElementBuffer (indices.Length);
			elementBuffer.SetData (indices);
			vertexBuffer.SetElementBuffer (elementBuffer);
		}

		static void Indices (int t0, int t1, int t2, int t3, int t4, int t5, ushort[] indices, ref int currVertex, ref int currIndex, int verticesPerSide)
		{
			for (int i = currVertex; i < (currVertex + verticesPerSide); i += 4) {
				//Triangle 1
				indices [currIndex++] = (ushort)(i + t0);
				indices [currIndex++] = (ushort)(i + t1);
				indices [currIndex++] = (ushort)(i + t2);
				//Triangle 2
				indices [currIndex++] = (ushort)(i + t3);
				indices [currIndex++] = (ushort)(i + t4);
				indices [currIndex++] = (ushort)(i + t5);
			}
			currVertex += verticesPerSide;
		}

		void TopBottom (int Y, int slices, VertexPositionTexture[] vertices, ref int vertexCount)
		{
			float posAdvance = 2f / (float)slices;
			float texInitialV = 1;
			float texAdvanceV = -(1f / (float)slices);
			float texInitialU = 0;
			float texAdvanceU = (1f / (float)slices);
			if (Y == -1) {
				texInitialV = 0;
				texAdvanceV = -texAdvanceV;
			}
			for (int x = 0; x < slices; x++) {
				for (int z = 0; z < slices; z++) {
					float advX = posAdvance * x;
					float advZ = posAdvance * z;

					float tadvX = texInitialU + (texAdvanceU * x);
					float tadvZ = texInitialV + (texAdvanceV * z); 
					//top-left
					vertices [vertexCount++] = new VertexPositionTexture (
						new Vector3 (-1 + advX, Y, -1 + advZ),
						new Vector2 (0 + tadvX, 0 + tadvZ)
					);
					//top-right
					vertices [vertexCount++] = new VertexPositionTexture (
						new Vector3 (-1 + advX + posAdvance, Y, -1 + advZ),
						new Vector2 (0 + tadvX + texAdvanceU, 0 + tadvZ)
					);
					//bottom-left
					vertices [vertexCount++] = new VertexPositionTexture (
						new Vector3 (-1 + advX, Y, -1 + advZ + posAdvance),
						new Vector2 (0 + tadvX, 0 + tadvZ + texAdvanceV)
					);
					//bottom-right
					vertices [vertexCount++] = new VertexPositionTexture (
						new Vector3 (-1 + advX + posAdvance, Y, -1 + advZ + posAdvance),
						new Vector2 (0 + tadvX + texAdvanceU, 0 + tadvZ + texAdvanceV)
					);
				}
			}
		}

		void FrontBack (int Z, int slices, VertexPositionTexture[] vertices, ref int vertexCount)
		{
			float posAdvance = 2f / (float)slices;
			float texInitialU = 0;
			float texAdvanceU = (1f / (float)slices);
			float texInitialV = 0;
			float texAdvanceV = (1f / (float)slices);
			if (Z == -1) {
				texInitialU = 1;
				texAdvanceU = -texAdvanceU;
			}
			for (int x = 0; x < slices; x++) {
				for (int y = 0; y < slices; y++) {
					float advX = posAdvance * x;
					float advY = posAdvance * y;

					float tadvX = texInitialU + (texAdvanceU * x);
					float tadvY = texInitialV + (texAdvanceV * y); 
					//top-left
					vertices [vertexCount++] = new VertexPositionTexture (
						new Vector3 (-1 + advX, -1 + advY, Z),
						new Vector2 (0 + tadvX, 0 + tadvY)
					);
					//top-right
					vertices [vertexCount++] = new VertexPositionTexture (
						new Vector3 (-1 + advX + posAdvance, -1 + advY, Z),
						new Vector2 (0 + tadvX + texAdvanceU, 0 + tadvY)
					);
					//bottom-left
					vertices [vertexCount++] = new VertexPositionTexture (
						new Vector3 (-1 + advX, -1 + advY + posAdvance, Z),
						new Vector2 (0 + tadvX, 0 + tadvY + texAdvanceV)
					);
					//bottom-right
					vertices [vertexCount++] = new VertexPositionTexture (
						new Vector3 (-1 + advX + posAdvance, -1 + advY + posAdvance, Z),
						new Vector2 (0 + tadvX + texAdvanceU, 0 + tadvY + texAdvanceV)
					);
				}
			}
		}

		void LeftRight (int X, int slices, VertexPositionTexture[] vertices, ref int vertexCount)
		{
			float posAdvance = 2f / (float)slices;
			float initialU = 0;
			float texAdvanceU = (1f / (float)slices);

			float initialV = 1;
			float texAdvanceV = -(1f / (float)slices);
			if (X == -1) {
				initialV = 0;
				texAdvanceV = -texAdvanceV;
			}

			for (int y = 0; y < slices; y++) {
				for (int z = 0; z < slices; z++) {
					float advY = posAdvance * y;
					float advZ = posAdvance * z;
					float tadvY = initialU + (texAdvanceU * y);
					float tadvZ = initialV + (texAdvanceV * z);
					//z = y
					//y = x
					//UV coords
					var tl = new Vector2 (tadvZ, tadvY);
					var tr = new Vector2 (tadvZ, tadvY + texAdvanceU);
					var bl = new Vector2 (tadvZ + texAdvanceV, tadvY);
					var br = new Vector2 (tadvZ + texAdvanceV, tadvY + texAdvanceU);
					//top-left
					vertices [vertexCount++] = new VertexPositionTexture (
						new Vector3 (X, -1 + advY, -1 + advZ),
						tl
					);
					//top-right
					vertices [vertexCount++] = new VertexPositionTexture (
						new Vector3 (X, -1 + advY + posAdvance, -1 + advZ),
						tr
					);
					//bottom-left
					vertices [vertexCount++] = new VertexPositionTexture (
						new Vector3 (X, -1 + advY, -1 + advZ + posAdvance),
						bl
					);
					//bottom-right
					vertices [vertexCount++] = new VertexPositionTexture (
						new Vector3 (X, -1 + advY + posAdvance, -1 + advZ + posAdvance),
						br
					);
				}
			}
		}

		public void Draw ()
		{
			vertexBuffer.Draw (PrimitiveTypes.TriangleList, 0, 0, primitiveCount);
		}

		public void Draw (CubeMapFace face)
		{
			vertexBuffer.Draw (PrimitiveTypes.TriangleList, 0, offsets [face], primitiveCountSide);
		}
	}
}

