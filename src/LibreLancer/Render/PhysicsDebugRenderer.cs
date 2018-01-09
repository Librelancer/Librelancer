using System;
using LibreLancer.Jitter;
using LibreLancer.Jitter.LinearMath;
using LibreLancer.Vertices;
namespace LibreLancer
{
	public class PhysicsDebugRenderer : IDebugDrawer, IDisposable
	{
		public Color4 Color = Color4.Red;
		const int MAX_LINES = 50000;
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
			DrawLineInternal(start, end);
		}

		public void DrawPoint(Vector3 pos)
		{
			DrawPointInternal(pos);
		}

		public void DrawTriangle(Vector3 pos1, Vector3 pos2, Vector3 pos3)
		{
			DrawTriangleInternal(pos1, pos2, pos3);
		}

		void DrawLineInternal(Vector3 start, Vector3 end)
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
			DrawLineInternal(pos1, pos2);
			DrawLineInternal(pos1, pos3);
			DrawLineInternal(pos3, pos2);
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

