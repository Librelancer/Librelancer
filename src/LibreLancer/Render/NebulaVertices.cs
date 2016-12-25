/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
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
			var vp = camera.ViewProjection;
			var z = RenderHelpers.GetZ(world, camera.Position, Vector3.Zero);
			buffer.AddCommand(
				shader,
				shaderDelegate,
				resetDelegate,
				world,
				new RenderUserData() { Color = color, ViewProjection = vp, Texture = texture },
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
		static Action<Shader, RenderState, RenderCommand> shaderDelegate = ShaderSetup;
		static void ShaderSetup(Shader shader, RenderState state, RenderCommand command)
		{
			state.Cull = false;
			state.BlendMode = BlendMode.Normal;
			shader.SetMatrix(_viewproj, ref command.UserData.ViewProjection);
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

