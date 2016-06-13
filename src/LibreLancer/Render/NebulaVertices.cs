using System;
using System.Runtime.InteropServices;
using LibreLancer.Vertices;

namespace LibreLancer
{
	public class NebulaVertices
	{
		const int MAX_QUADS = 300; //100 plane slices
		Shader shader;
		VertexBuffer vbo;
		ElementBuffer el;
		int currentVerts = 0;
		int currentIndex = 0;
		VertexPositionTexture[] verts;

		public NebulaVertices()
		{
			verts = new VertexPositionTexture[MAX_QUADS * 4];
			var indices = new ushort[MAX_QUADS * 6];
			int iptr = 0;
			for (int i = 0; i < verts.Length; i += 4)
			{
				/* Triangle 1 */
				indices[iptr++] = (ushort)i;
				indices[iptr++] = (ushort)(i + 1);
				indices[iptr++] = (ushort)(i + 2);
				/* Triangle 2 */
				indices[iptr++] = (ushort)(i + 1);
				indices[iptr++] = (ushort)(i + 3);
				indices[iptr++] = (ushort)(i + 2);
			}
			vbo = new VertexBuffer(typeof(VertexPositionTexture), verts.Length, true);
			el = new ElementBuffer(indices.Length);
			el.SetData(indices);
			vbo.SetElementBuffer(el);
			shader = ShaderCache.Get("NebulaInterior.vs", "NebulaInterior.frag");
		}

		public void SubmitQuad(
			VertexPositionTexture v1,
			VertexPositionTexture v2,
			VertexPositionTexture v3,
			VertexPositionTexture v4
		)
		{
			if (((currentVerts / 4) + 1) >= MAX_QUADS)
			{
				throw new Exception("NebulaVertices limit exceeded. Raise MAX_QUADS.");
			}
			currentIndex += 6;
			verts[currentVerts++] = v1;
			verts[currentVerts++] = v2;
			verts[currentVerts++] = v3;
			verts[currentVerts++] = v4;
		}

		public void Draw(RenderState rstate, ICamera camera, Texture texture, Color4 color, Matrix4 world)
		{
			var vp = camera.ViewProjection;
			shader.SetMatrix("ViewProjection", ref vp);
			shader.SetMatrix("World", ref world);
			shader.SetColor4("Tint", color);
			shader.SetInteger("Texture", 0);
			texture.BindTo(0);
			shader.UseProgram();
			rstate.BlendMode = BlendMode.Normal;
			rstate.Cull = false;
			vbo.SetData(verts, currentVerts);
			vbo.Draw(PrimitiveTypes.TriangleList, currentIndex / 3);
			rstate.Cull = true;
			currentVerts = 0;
			currentIndex = 0;
		}
	}
}

