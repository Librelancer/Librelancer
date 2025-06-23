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
        private QuadMaterial material;
		public PolylineRender(RenderContext rstate, CommandBuffer buffer)
        {
            material = new QuadMaterial(null);
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

        public void StartQuadLine(Texture2D tex, ushort blend)
        {
            if (pointsCount != 0)
                throw new Exception("Polyline bad state");
            texture = tex;
            this.blend = blend;
        }

        public void AddQuad(Vector3 p0, Vector3 p1, Color4 color)
        {
            //var mid = (p0 + p1) / 2;
            //var face = QuaternionEx.LookAt(p0, p1);
            var dir = (p1 - p0).Normalized();
            var scale = (p1 - p0).Length();

            Vector3 up = MathF.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;
            var perp = Vector3.Cross(dir, up).Normalized();

            const float SCALE_ENDS = 0.2f;
            var top = p1 + (dir * scale * SCALE_ENDS);
            var bottom = p0;

            var mid = (bottom + top) * 0.5f;

            var left = mid + -perp * (scale * 0.5f);
            var right = mid + perp * (scale * 0.5f);

            const float MARGIN = 0.02f;

            const float T0 = 0 + MARGIN;
            const float T1 = 1 - MARGIN;
            Vector2 tTop = new(T0, T0);
            Vector2 tBottom = new(T1, T1);
            Vector2 tLeft = new(T0, T1);
            Vector2 tRight = new(T1, T0);


            vertices[vertexCount++] = new VertexPositionColorTexture(
                top,
                color,
                tTop);

            vertices[vertexCount++] = new VertexPositionColorTexture(
                bottom,
                color,
                tBottom);

            vertices[vertexCount++] = new VertexPositionColorTexture(
                right,
                color,
                tRight);

            vertices[vertexCount++] = new VertexPositionColorTexture(
                top,
                color,
                tTop);

            vertices[vertexCount++] = new VertexPositionColorTexture(
                bottom,
                color,
                tBottom);

            vertices[vertexCount++] = new VertexPositionColorTexture(
                left,
                color,
                tLeft);

            pointsCount += 6;
        }

        public void FinishQuadLine(float z)
        {
            int startPos = vertexCount - pointsCount;

            buffer.AddCommand(
                material, null, buffer.WorldBuffer.Identity, Lighting.Empty,
                vbo,
                PrimitiveTypes.TriangleList,
                -1,
                startPos,
                (pointsCount) / 3,
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
