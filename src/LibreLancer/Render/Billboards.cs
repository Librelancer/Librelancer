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
	public class Billboards
	{
		const int MAX_BILLBOARDS = 4096;

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

			public VertexDeclaration GetVertexDeclaration()
			{
				return new VertexDeclaration (
					18 * sizeof(float),
					new VertexElement (VertexSlots.Position, 3, VertexElementType.Float, false, 0),
					new VertexElement (VertexSlots.Size, 2, VertexElementType.Float, false, sizeof(float) * 3),
					new VertexElement (VertexSlots.Color, 4, VertexElementType.Float, false, sizeof(float) * 5),
					new VertexElement (VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 9),
					new VertexElement (VertexSlots.Texture2, 2, VertexElementType.Float, false, sizeof(float) * 11),
					new VertexElement (VertexSlots.Texture3, 2, VertexElementType.Float, false, sizeof(float) * 13),
					new VertexElement (VertexSlots.Texture4, 2, VertexElementType.Float, false, sizeof(float) * 15),
					new VertexElement (VertexSlots.Angle, 1, VertexElementType.Float, false, sizeof(float) * 17)
				);
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
		CommandBuffer buffer;
		public void Begin(ICamera cam, CommandBuffer cmd)
		{
			camera = cam;
			currentTexture = null;
			billboardCount = lastCount = 0;
			buffer = cmd;
		}

		public void DrawCustomShader(
			string shader,
			RenderUserData userData,
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
			Flush();
			var sh = ShaderCache.Get(
				"Billboard.vs",
				shader,
				"Billboard.gs"
			);
			currentTexture = null;
			vertices[billboardCount].Position = Position;
			vertices[billboardCount].Size = size;
			vertices[billboardCount].Color = color;
			vertices[billboardCount].Texture0 = topleft;
			vertices[billboardCount].Texture1 = topright;
			vertices[billboardCount].Texture2 = bottomleft;
			vertices[billboardCount].Texture3 = bottomright;
			vertices[billboardCount].Angle = angle;
			//increase count
			billboardCount++;
			var dat = userData;
			dat.ViewProjection = camera.ViewProjection;
			buffer.AddCommand(
				sh,
				_setupDelegateCustom,
				_resetDelegate,
				camera.View,
				dat,
				vbo,
				PrimitiveTypes.Points,
				lastCount,
				billboardCount,
				true
			);
			lastCount = billboardCount;
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
				throw new Exception("Billboard overflow");
			currentTexture = texture;
			//setup vertex
			vertices [billboardCount].Position = Position;
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

		int lastCount = 0;
		void Flush()
		{
			if (billboardCount == 0 || lastCount == billboardCount)
				return;
			var view = camera.View;
			var vp = camera.ViewProjection;
			buffer.AddCommand(
				shader,
				_setupDelegate,
				_resetDelegate,
				view,
				new RenderUserData() { Texture = currentTexture, ViewProjection = vp },
				vbo,
				PrimitiveTypes.Points,
				lastCount,
				billboardCount,
				true
			);
			lastCount = billboardCount;

			currentTexture = null;
		}
		static Action<Shader, RenderState, RenderCommand> _setupDelegate = SetupShader;
		static void SetupShader(Shader shader, RenderState rs, RenderCommand cmd)
		{
			rs.Cull = false;
			rs.BlendMode = BlendMode.Normal;
			shader.SetMatrix("View", ref cmd.World);
			shader.SetMatrix("ViewProjection", ref cmd.UserData.ViewProjection);
			cmd.UserData.Texture.BindTo(0);
		}

		static Action<Shader, RenderState, RenderCommand> _setupDelegateCustom = SetupShaderCustom;
		static void SetupShaderCustom(Shader shader, RenderState rs, RenderCommand cmd)
		{
			rs.Cull = false;
			rs.BlendMode = BlendMode.Normal;
			shader.SetMatrix("View", ref cmd.World);
			shader.SetMatrix("ViewProjection", ref cmd.UserData.ViewProjection);
			cmd.UserData.UserFunction(shader, cmd.UserData);
		}

		static Action<RenderState> _resetDelegate = ResetState;
		static void ResetState(RenderState rs)
		{
			rs.Cull = true;
		}
		public void End()
		{
			Flush ();
			vbo.SetData(vertices, billboardCount);
		}
	}
}

