// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Render.Materials;

namespace LibreLancer.Render
{
	public unsafe class PolylineRender : IDisposable
	{
		const int MAX_VERTICES = 32768;

        VertexPositionColorTexture* vertices;
		VertexBuffer vbo;
        CommandBuffer buffer;
        private PolylineMaterial material;
		public PolylineRender(RenderContext rstate, CommandBuffer buffer)
        {
            material = new PolylineMaterial(null);
			vbo = new VertexBuffer(rstate, typeof(VertexPositionColorTexture), MAX_VERTICES, true);
			this.buffer = buffer;
		}

        public void StartFrame()
        {
            material.Parameters.Clear();
            vertices = (VertexPositionColorTexture*)vbo.BeginStreaming();
        }


        public void StartLine(Texture2D tex, ushort blend)
		{
			if (pointsCount != 0)
				throw new Exception("Polyline bad state");
			texture = tex;
			this.blend = blend;
		}
		ushort blend;
		Texture2D texture;
		int vertexCount = 0;
		int pointsCount = 0;
		public void AddPoint(Vector3 a, Vector3 b, Vector2 uv1, Vector2 uv2, Color4 color)
        {
            if (vertices == (VertexPositionColorTexture*) 0)
                throw new InvalidOperationException();
			vertices[vertexCount++] = new VertexPositionColorTexture(
				a,
				color,
				uv1);
			vertices[vertexCount++] = new VertexPositionColorTexture(
				b,
				color,
				uv2
			);
			pointsCount++;
		}

		public void FinishLine(float z)
		{
			if (pointsCount < 2)
			{
				vertexCount -= pointsCount * 2;
				pointsCount = 0;
				return;
			}

			int startPos = vertexCount - (pointsCount * 2);

			buffer.AddCommand(
                material, null, buffer.WorldBuffer.Identity, Lighting.Empty,
				vbo,
				PrimitiveTypes.TriangleStrip,
                -1,
				startPos,
				(pointsCount - 1) * 2,
				SortLayers.OBJECT,
				z, null, 0, material.Parameters.Count
			);
            material.Parameters.Add((texture, blend));
			pointsCount = 0;
		}

		public void EndFrame()
		{
			vbo.EndStreaming(vertexCount);
            vertices = (VertexPositionColorTexture*) 0;
            vertexCount = 0;
		}

        public void Dispose()
		{
			vbo.Dispose();
		}
	}
}
