// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Vertices;
namespace LibreLancer
{
	public class PolylineRender : IDisposable
	{
		const int MAX_VERTICES = 8192;

		VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[MAX_VERTICES];
		VertexBuffer vbo;
		static ShaderVariables shader;
		CommandBuffer buffer;
		public PolylineRender(CommandBuffer buffer)
		{
			if (shader == null)
			{
				shader = ShaderCache.Get(
					"Polyline.vs",
					"Billboard.frag"
				);
				shader.Shader.SetInteger(shader.Shader.GetLocation("tex0"), 0);
			}
			vbo = new VertexBuffer(typeof(VertexPositionColorTexture), MAX_VERTICES, true);
			this.buffer = buffer;
		}


		ICamera camera;
		public void SetCamera(ICamera cam)
		{
			camera = cam;
		}

		public void StartLine(Texture2D tex, BlendMode blend)
		{
			if (pointsCount != 0)
				throw new Exception("Polyline bad state");
			texture = tex;
			this.blend = blend;
		}
		BlendMode blend;
		Texture2D texture;
		int vertexCount = 0;
		int pointsCount = 0;
		public void AddPoint(Vector3 a, Vector3 b, Vector2 uv1, Vector2 uv2, Color4 color)
		{
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
				shader.Shader,
				Setup,
				Cleanup,
				Matrix4.Identity,
				new RenderUserData() { Texture = texture, Camera = camera, Float = (float)(int)blend },
				vbo,
				PrimitiveTypes.TriangleStrip,
				startPos,
				(pointsCount - 1) * 2,
				true,
				SortLayers.OBJECT,
				z
			);
			pointsCount = 0;
		}

		public void FrameEnd()
		{
			vbo.SetData(vertices, vertexCount);
			vertexCount = 0;
		}
		static void Setup(Shader shdr, RenderState res, ref LibreLancer.RenderCommand cmd)
		{
			shader.SetViewProjection(cmd.UserData.Camera);
			cmd.UserData.Texture.BindTo(0);
			res.BlendMode = (BlendMode)(int)cmd.UserData.Float;
			res.Cull = false;
		}
				
		static void Cleanup(RenderState rs)
		{
			rs.Cull = true;
		}

		public void Dispose()
		{
			vbo.Dispose();
		}
	}
}
