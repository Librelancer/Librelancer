// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Utf.Cmp;

namespace LibreLancer.Render
{
	public class LineRenderer :  IDisposable, Physics.IDebugRenderer
	{
		const int MAX_LINES = 65536;
		VertexPositionColor[] lines = new VertexPositionColor[MAX_LINES * 2];
		VertexBuffer linebuffer;
		int lineVertices = 0;
        public string SkeletonHighlight;

		Shaders.ShaderVariables shader;
		public LineRenderer(RenderContext rstate)
        {
            shader = Shaders.PhysicsDebug.Get(rstate);
			linebuffer = new VertexBuffer(rstate, typeof(VertexPositionColor), MAX_LINES * 2, true);
		}

		RenderContext rstate;
		public void StartFrame(RenderContext rs)
		{
			rstate = rs;
			lineVertices = 0;
		}

        public void DrawLine(Vector3 start, Vector3 end, Color4 color)
        {
            if (lineVertices + 2 >= lines.Length)
            {
                Render();
                lineVertices = 0;
            }
            lines[lineVertices++] = new VertexPositionColor(start, color);
            lines[lineVertices++] = new VertexPositionColor(end, color);
        }

		public void DrawPoint(Vector3 pos, Color4 color)
        {
            if ((lineVertices % 2 == 0) &&
                (lineVertices + 2) >= lines.Length)
            {
                Render();
                lineVertices = 0;
            }
            lines[lineVertices++] = new VertexPositionColor(pos, color);
        }

        public void DrawTriangleMesh(Matrix4x4 mat, Vector3[] positions, int[] indices, Color4 color)
        {
            for(int i = 1; i < indices.Length; i++)
            {
                var p1 = positions[indices[i - 1]];
                var p2 = positions[indices[i]];
                DrawLine(Vector3.Transform(p1, mat), Vector3.Transform(p2, mat), color);
            }
        }

        public void DrawVWire(VMeshWire wire, VertexResource resource, Matrix4x4 world, Color4 color)
        {
            Render();
            rstate.Cull = false;
            rstate.DepthWrite = false;
            rstate.PolygonOffset = Vector2.One;
            shader.SetWorld(ref world, ref world);
            shader.SetDc(color);
            shader.SetOc(1);
            rstate.Shader = shader;
            resource.VertexBuffer.DrawImmediateElements(
                PrimitiveTypes.LineList,
                wire.VertexOffset + resource.BaseVertex,
                wire.Indices
            );
            rstate.PolygonOffset = Vector2.Zero;
            rstate.DepthWrite = true;
            rstate.Cull = true;
        }

        public void Render()
		{
			if (lineVertices == 0)
				return;
			rstate.Cull = false;
            rstate.DepthWrite = false;
            rstate.PolygonOffset = Vector2.One;
            var bm = rstate.BlendMode;
            rstate.BlendMode = BlendMode.Normal;
            var w = Matrix4x4.Identity;
            shader.SetWorld(ref w, ref w);
            shader.SetOc(0);
            rstate.Shader = shader;
			linebuffer.SetData<VertexPositionColor>(lines.AsSpan().Slice(0, lineVertices));
			linebuffer.Draw(PrimitiveTypes.LineList, lineVertices / 2);
            rstate.PolygonOffset = Vector2.Zero;
            rstate.DepthWrite = true;
            rstate.BlendMode = bm;
            rstate.Cull = true;
        }

		public void Dispose()
		{
			linebuffer.Dispose();
		}
	}
}

