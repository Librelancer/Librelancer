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
		RenderData[] rendat;
		VertexBuffer vbo;
		ushort[] indices = new ushort[MAX_BILLBOARDS];
		ElementBuffer ibo;
		public Billboards ()
		{
			shader = ShaderCache.Get (
				"Billboard.vs",
				"Billboard.frag",
				"Billboard.gs"
			);
			shader.SetInteger ("tex0", 0);
			vertices = new BVert[MAX_BILLBOARDS];
			rendat = new RenderData[MAX_BILLBOARDS];
			vbo = new VertexBuffer (typeof(BVert), MAX_BILLBOARDS, true);
			ibo = new ElementBuffer(MAX_BILLBOARDS);
		}
		struct RenderData
		{
			public Texture Texture;
			public BlendMode BlendMode;
			public RenderData(Texture tex, BlendMode blend)
			{
				Texture = tex;
				BlendMode = blend;
			}
			public override int GetHashCode()
			{
				unchecked
				{
					int hash = 17;
					hash += hash * 23 + Texture.GetHashCode();
					hash += hash * 23 + BlendMode.GetHashCode();
					return hash;
				}
			}
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
			float angle,
			int layer,
			float z = float.NegativeInfinity
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
				1,
				true,
				layer,
				float.IsNegativeInfinity(z) ? RenderHelpers.GetZ(Matrix4.Identity, camera.Position, Position) : z
			);
			lastCount = billboardCount;
		}
		int currentLayer = 0;
		int bmode = 0;
		public void Draw(
			Texture2D texture,
			Vector3 Position,
			Vector2 size,
			Color4 color,
			Vector2 topleft,
			Vector2 topright,
			Vector2 bottomleft,
			Vector2 bottomright,
			float angle,
			int layer,
			BlendMode blend = BlendMode.Normal
		)
		{
			/*if (currentTexture != texture && currentTexture != null)
				Flush ();
			if (billboardCount + 1 > MAX_BILLBOARDS)
				throw new Exception("Billboard overflow");*/
			//bmode = (int)blend;
			//Flush();
			//currentTexture = texture;
			//currentLayer = layer;
			//setup vertex
			vertices [billboardCount].Position = Position;
			vertices [billboardCount].Size = size;
			vertices [billboardCount].Color = color;
			vertices [billboardCount].Texture0 = topleft;
			vertices [billboardCount].Texture1 = topright;
			vertices [billboardCount].Texture2 = bottomleft;
			vertices [billboardCount].Texture3 = bottomright;
			vertices [billboardCount].Angle = angle;
			rendat[billboardCount].Texture = texture;
			rendat[billboardCount].BlendMode = blend;
			var z = RenderHelpers.GetZ(camera.Position, Position);
			buffer.AddCommand(
				this,
				rendat[billboardCount].GetHashCode(),
				billboardCount,
				layer,
				z
			);
			//increase count
			billboardCount++;
			lastCount = billboardCount;
		}

		int lastCount = 0;
		void Flush()
		{
			if (billboardCount == 0 || lastCount == billboardCount || currentTexture == null)
				return;
			var view = camera.View;
			var vp = camera.ViewProjection;
			Vector3 avgPos = Vector3.Zero;
			for (int i = lastCount; i < billboardCount; i++)
				avgPos += vertices[i].Position;
			avgPos /= (billboardCount - lastCount);
			var z = RenderHelpers.GetZ(camera.Position, avgPos);
			buffer.AddCommand(
				shader,
				_setupDelegate,
				_resetDelegate,
				view,
				new RenderUserData() { Texture = currentTexture, ViewProjection = vp, Integer = (int)bmode },
				vbo,
				PrimitiveTypes.Points,
				lastCount,
				billboardCount - lastCount,
				true,
				currentLayer,
				z
			);
			lastCount = billboardCount;
			currentLayer = 0;
			currentTexture = null;
		}
		int lasthash = -1;
		int datindex = 0;
		int indexCount = 0;

		public void RenderStandard(int index, int hash, RenderState rs)
		{
			if (hash != lasthash && lasthash != -1)
				FlushCommands(rs);
			lasthash = hash;
			datindex = index;
			indices[indexCount++] = (ushort)index;
		}

		public void FlushCommands(RenderState rs)
		{
			if (indexCount == 0)
			{
				lasthash = -1;
				return;
			}
			ibo.SetData(indices, indexCount);
			vbo.SetElementBuffer(ibo);
			rs.Cull = false;
			rs.BlendMode = rendat[datindex].BlendMode;
			var v = camera.View;
			var vp = camera.ViewProjection;
			shader.SetMatrix("View", ref v);
			shader.SetMatrix("ViewProjection", ref vp);
			rendat[datindex].Texture.BindTo(0);
			shader.UseProgram();
			vbo.Draw(PrimitiveTypes.Points, 0, 0, indexCount);
			rs.Cull = false;
			vbo.UnsetElementBuffer();
			lasthash = -1;
			indexCount = 0;
		}

		static Action<Shader, RenderState, RenderCommand> _setupDelegate = SetupShader;
		static void SetupShader(Shader shader, RenderState rs, RenderCommand cmd)
		{
			rs.Cull = false;
			rs.BlendMode = (BlendMode)cmd.UserData.Integer;
			shader.SetMatrix("View", ref cmd.World);
			shader.SetMatrix("ViewProjection", ref cmd.UserData.ViewProjection);
			cmd.UserData.Texture.BindTo(0);
		}

		static Action<Shader, RenderState, RenderCommand> _setupDelegateCustom = SetupShaderCustom;
		static void SetupShaderCustom(Shader shdr, RenderState rs, RenderCommand cmd)
		{
			rs.Cull = false;
			rs.BlendMode = BlendMode.Normal;
			cmd.UserData.UserFunction(shdr,rs, cmd.UserData);
			shdr.SetMatrix("View", ref cmd.World);
			shdr.SetMatrix("ViewProjection", ref cmd.UserData.ViewProjection);
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

