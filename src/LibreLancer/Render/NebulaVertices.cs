// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
using LibreLancer.Vertices;

namespace LibreLancer
{
	public class NebulaVertices
	{
		const int MAX_QUADS = 3000; //1000 plane slices
		Shader shader;
		VertexBuffer vbo;
		ElementBuffer el;
		int currentVerts = 0;
		int currentIndex = 0;
		VertexPositionTexture[] verts;
		static int _viewproj;
		static int _world;
		static int _tint;
		static int _texture;
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
			shader = ShaderCache.Get("NebulaInterior.vs", "NebulaInterior.frag").Shader;
			_viewproj = shader.GetLocation("ViewProjection");
			_world = shader.GetLocation("World");
			_tint = shader.GetLocation("Tint");
			_texture = shader.GetLocation("Texture");
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
		int lastIndex = 0;
		public void Draw(CommandBuffer buffer, ICamera camera, Texture texture, Color4 color, Matrix4 world, bool inside)
		{
            if(texture == null) {
                lastIndex = currentIndex;
                return;
            }
			var z = RenderHelpers.GetZ(world, camera.Position, Vector3.Zero);
			buffer.AddCommand(
				shader,
				shaderDelegate,
				resetDelegate,
				world,
				new RenderUserData() { Color = color, Camera = camera, Texture = texture },
				vbo,
				PrimitiveTypes.TriangleList,
				0,
				lastIndex,
				(currentIndex - lastIndex) / 3,
				true,
				inside ? SortLayers.NEBULA_INSIDE : SortLayers.NEBULA_NORMAL,
				z
			);
			lastIndex = currentIndex;
		}
		static ShaderAction shaderDelegate = ShaderSetup;
		static void ShaderSetup(Shader shader, RenderState state, ref RenderCommand command)
		{
			state.Cull = false;
			state.BlendMode = BlendMode.Normal;
			var vp = command.UserData.Camera.ViewProjection;
			shader.SetMatrix(_viewproj, ref vp);
			shader.SetMatrix(_world, ref command.World);
			shader.SetColor4(_tint, command.UserData.Color);
			shader.SetInteger(_texture, 0);
			command.UserData.Texture.BindTo(0);
		}
		static Action<RenderState> resetDelegate = ResetState;
		static void ResetState(RenderState state)
		{
			state.Cull = true;
		}
		public void SetData()
		{
			vbo.SetData(verts, currentVerts);
		}
		public void NewFrame()
		{
			lastIndex = currentIndex = currentVerts = 0;
		}
	}
}

