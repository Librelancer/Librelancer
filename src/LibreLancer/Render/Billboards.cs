using System;
using OpenTK;
using OpenTK.Graphics;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using LibreLancer.Vertices;
namespace LibreLancer
{
	public class Billboards
	{
		const int MAX_BILLBOARDS = 1024;

		[StructLayout(LayoutKind.Sequential)]
		struct BVert : IVertexType
		{
			public Vector3 Position;
			public Vector2 Size;
			public Color4 Color;
			public Vector2 Texture0;
			public Vector2 Texture1;
			public Vector2 Texture2;
			public Vector2 Texture3;
			public float Angle;
			public void SetVertexPointers (int offset)
			{
				GL.EnableVertexAttribArray(VertexSlots.Position);
				GL.EnableVertexAttribArray (VertexSlots.Size);
				GL.EnableVertexAttribArray(VertexSlots.Color);
				GL.EnableVertexAttribArray (VertexSlots.Texture1);
				GL.EnableVertexAttribArray (VertexSlots.Texture2);
				GL.EnableVertexAttribArray (VertexSlots.Texture3);
				GL.EnableVertexAttribArray (VertexSlots.Texture4);
				GL.EnableVertexAttribArray (VertexSlots.Angle);

				GL.VertexAttribPointer(VertexSlots.Position, 3, VertexAttribPointerType.Float, false, VertexSize(), offset);
				GL.VertexAttribPointer(VertexSlots.Size, 2, VertexAttribPointerType.Float, false, VertexSize(), offset + sizeof(float) * 3);
				GL.VertexAttribPointer (VertexSlots.Color, 4, VertexAttribPointerType.Float, false, VertexSize (), offset + sizeof(float) * 5);
				GL.VertexAttribPointer (VertexSlots.Texture1, 2, VertexAttribPointerType.Float, false, VertexSize (), offset + sizeof(float) * 9);
				GL.VertexAttribPointer (VertexSlots.Texture2, 2, VertexAttribPointerType.Float, false, VertexSize (), offset + sizeof(float) * 11);
				GL.VertexAttribPointer (VertexSlots.Texture3, 2, VertexAttribPointerType.Float, false, VertexSize (), offset + sizeof(float) * 13);
				GL.VertexAttribPointer (VertexSlots.Texture4, 2, VertexAttribPointerType.Float, false, VertexSize (), offset + sizeof(float) * 15);
				GL.VertexAttribPointer (VertexSlots.Angle, 1, VertexAttribPointerType.Float, false, VertexSize (), offset + sizeof(float) * 17);
			}
			public int VertexSize ()
			{
				return 18 * sizeof(float);
			}
		}

		Shader shader;
		BVert[] vertices;
		VertexBuffer vbo;
		public Billboards ()
		{
			shader = ShaderCache.Get (
				"Billboard.vs",
				"Billboard.frag",
				"Billboard.gs"
			);
			shader.SetInteger ("tex0", 0);
			vertices = new BVert[MAX_BILLBOARDS];
			vbo = new VertexBuffer (typeof(BVert), MAX_BILLBOARDS, true);
		}

		ICamera camera;
		Texture2D currentTexture;
		int billboardCount = 0;
		public void Begin(ICamera cam)
		{
			camera = cam;
			currentTexture = null;
			billboardCount = 0;
		}

		public void Draw(
			Texture2D texture,
			Vector3 Position,
			Vector2 size,
			Color4 color,
			Vector2 topleft,
			Vector2 topright,
			Vector2 bottomleft,
			Vector2 bottomright,
			float angle
		)
		{
			if (currentTexture != texture && currentTexture != null)
				Flush ();
			if (billboardCount + 1 > MAX_BILLBOARDS)
				Flush ();
			currentTexture = texture;
			//setup vertex
			vertices[billboardCount].Position = Position;
			vertices [billboardCount].Size = size;
			vertices [billboardCount].Color = color;
			vertices [billboardCount].Texture0 = topleft;
			vertices [billboardCount].Texture1 = topright;
			vertices [billboardCount].Texture2 = bottomleft;
			vertices [billboardCount].Texture3 = bottomright;
			vertices [billboardCount].Angle = angle;
			//increase count
			billboardCount++;
		}

		void Flush()
		{
			if (billboardCount == 0)
				return;
			
			var view = camera.View;
			var vp = camera.ViewProjection;
			shader.SetMatrix ("View", ref view);
			shader.SetMatrix ("ViewProjection", ref vp);
			currentTexture.BindTo (TextureUnit.Texture0);
			shader.UseProgram ();
			//draw
			GL.Disable (EnableCap.CullFace);
			GL.Enable (EnableCap.Blend);
			vbo.SetData(vertices, billboardCount);
			vbo.Draw (PrimitiveTypes.Points, billboardCount);
			GL.Enable (EnableCap.CullFace);
			//blah
			currentTexture = null;
			billboardCount = 0;
		}

		public void End()
		{
			Flush ();
		}
	}
}

