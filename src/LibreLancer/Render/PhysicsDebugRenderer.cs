// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Vertices;
namespace LibreLancer
{
	public class PhysicsDebugRenderer :  IDisposable, Physics.IDebugRenderer
	{
		public Color4 Color = Color4.Red;
		const int MAX_LINES = 200000;
		VertexPositionColor[] lines = new VertexPositionColor[MAX_LINES * 2];
		VertexBuffer linebuffer;
		int lineVertices = 0;

		ShaderVariables shader;
		public PhysicsDebugRenderer()
		{
			shader = ShaderCache.Get("physicsdebug.vs", "physicsdebug.frag");
			linebuffer = new VertexBuffer(typeof(VertexPositionColor), MAX_LINES * 2, true);
		}

		ICamera camera;
		RenderState rstate;
		public void StartFrame(ICamera cam, RenderState rs)
		{
			camera = cam;
			rstate = rs;
			lineVertices = 0;	
		}

		public void DrawLine(Vector3 start, Vector3 end)
		{
            DrawLineInternal(start, end, Color);
		}

        public void DrawLine(Vector3 start, Vector3 end, Color4 color)
        {
            DrawLineInternal(start, end, color);
        }
		public void DrawPoint(Vector3 pos)
		{
			DrawPointInternal(pos);
		}

		public void DrawTriangle(Vector3 pos1, Vector3 pos2, Vector3 pos3)
		{
			DrawTriangleInternal(pos1, pos2, pos3);
		}

        void DrawLineInternal(Vector3 start, Vector3 end, Color4 color)
		{
			if ((lineVertices * 2) + 1 >= MAX_LINES)
			{
				Render();
				lineVertices = 0;
			}
			lines[lineVertices++] = new VertexPositionColor(start, Color);
			lines[lineVertices++] = new VertexPositionColor(end, Color);
		}

		void DrawPointInternal(Vector3 pos)
		{

		}

		void DrawTriangleInternal(Vector3 pos1, Vector3 pos2, Vector3 pos3)
		{
            DrawLineInternal(pos1, pos2, Color);
            DrawLineInternal(pos1, pos3, Color);
			DrawLineInternal(pos3, pos2, Color);
		}

		public void Render()
		{
			if (lineVertices == 0)
				return;
			rstate.Cull = false;
			rstate.DepthEnabled = false;
			var vp = camera.ViewProjection;
			shader.SetViewProjection(ref vp);
			shader.UseProgram();
			linebuffer.SetData(lines, lineVertices);
			linebuffer.Draw(PrimitiveTypes.LineList, lineVertices / 2);
			rstate.Cull = true;
			rstate.DepthEnabled = true;
		}

		public void Dispose()
		{
			linebuffer.Dispose();
		}
	}
}

