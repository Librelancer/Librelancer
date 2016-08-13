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
			public Vector2 TexCoord;
			public float Angle;

			public VertexDeclaration GetVertexDeclaration()
			{
				return new VertexDeclaration(
					12 * sizeof(float),
					new VertexElement(VertexSlots.Position, 3, VertexElementType.Float, false, 0),
					new VertexElement(VertexSlots.Size, 2, VertexElementType.Float, false, sizeof(float) * 3),
					new VertexElement(VertexSlots.Color, 4, VertexElementType.Float, false, sizeof(float) * 5),
					new VertexElement(VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 9),
					new VertexElement(VertexSlots.Angle, 1, VertexElementType.Float, false, sizeof(float) * 11)
				);
			}

		}

		Shader shader;
		BVert[] vertices;
		RenderData[] rendat;
		VertexBuffer vbo;
		ushort[] indices = new ushort[MAX_BILLBOARDS * 6];
		ushort[] single_indices = new ushort[6];
		ElementBuffer ibo;
		public Billboards()
		{
			shader = ShaderCache.Get(
				"Billboard.vs",
				"Billboard.frag"
			);
			shader.SetInteger("tex0", 0);
			vertices = new BVert[MAX_BILLBOARDS * 4];
			rendat = new RenderData[MAX_BILLBOARDS];
			vbo = new VertexBuffer(typeof(BVert), MAX_BILLBOARDS * 4, true);
			ibo = new ElementBuffer(MAX_BILLBOARDS * 6);
			vbo.SetElementBuffer(ibo);
		}
		struct RenderData
		{
			public Texture Texture;
			public BlendMode BlendMode;
			public ushort Index0;
			public ushort Index1;
			public ushort Index2;
			public ushort Index3;
			public ushort Index4;
			public ushort Index5;
			public RenderData(Texture tex, BlendMode blend, ushort idxStart)
			{
				Texture = tex;
				BlendMode = blend;
				Index0 = idxStart;
				Index1 = (ushort)(idxStart + 1);
				Index2 = (ushort)(idxStart + 2);
				Index3 = (ushort)(idxStart + 1);
				Index4 = (ushort)(idxStart + 3);
				Index5 = (ushort)(idxStart + 2);
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
			billboardCount = vertexCount = indexCount = 0;
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
			var sh = ShaderCache.Get(
				"Billboard.vs",
				shader
			);
			currentTexture = null;
			var dat = userData;
			dat.ViewProjection = camera.ViewProjection;
			dat.Integer = vertexCount;
			dat.Object = single_indices;
			CreateBillboard(
				Position,
				size,
				color,
				angle,
				topleft,
				topright,
				bottomleft,
				bottomright
			);
			buffer.AddCommand(
				sh,
				_setupDelegateCustom,
				_resetDelegate,
				camera.View,
				dat,
				vbo,
				PrimitiveTypes.TriangleList,
				0,
				2,
				true,
				layer,
				float.IsNegativeInfinity(z) ? RenderHelpers.GetZ(Matrix4.Identity, camera.Position, Position) : z
			);
		}
		void CreateBillboard(Vector3 position, Vector2 size, Color4 color, float angle, Vector2 topleft, Vector2 topright, Vector2 bottomleft, Vector2 bottomright)
		{
			vertices[vertexCount++] = new BVert()
			{
				Position = position,
				Size = size * -0.5f,
				Angle = angle,
				Color = color,
				TexCoord = bottomleft
			};
			vertices[vertexCount++] = new BVert()
			{
				Position = position,
				Size = size * new Vector2(0.5f, -0.5f),
				Angle = angle,
				Color = color,
				TexCoord = topleft
			};
			vertices[vertexCount++] = new BVert()
			{
				Position = position,
				Size = size * new Vector2(-0.5f, 0.5f),
				Angle = angle,
				Color = color,
				TexCoord = bottomright
			};
			vertices[vertexCount++] = new BVert()
			{
				Position = position,
				Size = size * 0.5f,
				Angle = angle,
				Color = color,
				TexCoord = topright
			};
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
			float angle,
			int layer,
			BlendMode blend = BlendMode.Normal
		)
		{
			rendat[billboardCount] = new RenderData(
				texture,
				blend,
				(ushort)vertexCount
			);
			CreateBillboard(
				Position,
				size,
				color,
				angle,
				topleft,
				topright,
				bottomleft,
				bottomright
			);
			var z = RenderHelpers.GetZ(camera.Position, Position);
			buffer.AddCommand(
				this,
				rendat[billboardCount].GetHashCode(),
				billboardCount,
				layer,
				z
			);
			billboardCount++;
		}

		int lasthash = -1;
		int datindex = 0;
		int indexCount = 0;
		int vertexCount = 0;

		public void RenderStandard(int index, int hash, RenderState rs)
		{
			if (hash != lasthash && lasthash != -1)
				FlushCommands(rs);
			lasthash = hash;
			datindex = index;
			var dat = rendat[index];
			indices[indexCount++] = dat.Index0;
			indices[indexCount++] = dat.Index1;
			indices[indexCount++] = dat.Index2;
			indices[indexCount++] = dat.Index3;
			indices[indexCount++] = dat.Index4;
			indices[indexCount++] = dat.Index5;
			_frameStart = true;
		}
		bool _frameStart = true;
		public void FlushCommands(RenderState rs)
		{
			if (indexCount == 0)
			{
				lasthash = -1;
				return;
			}
			ibo.SetData(indices, indexCount);
			rs.Cull = false;
			rs.BlendMode = rendat[datindex].BlendMode;
			if (_frameStart)
			{
				var v = camera.View;
				var vp = camera.ViewProjection;
				shader.SetMatrix("View", ref v);
				shader.SetMatrix("ViewProjection", ref vp);
				_frameStart = false;
			}
			rendat[datindex].Texture.BindTo(0);
			shader.UseProgram();
			vbo.Draw(PrimitiveTypes.TriangleList, 0, 0, indexCount / 3);
			rs.Cull = true;
			lasthash = -1;
			indexCount = 0;
		}

		static Action<Shader, RenderState, RenderCommand> _setupDelegateCustom = SetupShaderCustom;
		static void SetupShaderCustom(Shader shdr, RenderState rs, RenderCommand cmd)
		{
			rs.Cull = false;
			rs.BlendMode = BlendMode.Normal;
			cmd.UserData.UserFunction(shdr,rs, cmd.UserData);
			shdr.SetMatrix("View", ref cmd.World);
			shdr.SetMatrix("ViewProjection", ref cmd.UserData.ViewProjection);
			int idxStart = cmd.UserData.Integer;
			var indices = (ushort[])cmd.UserData.Object;
			indices[0] = (ushort)idxStart;
			indices[1] = (ushort)(idxStart + 1);
			indices[2] = (ushort)(idxStart + 2);
			indices[3] = (ushort)(idxStart + 1);
			indices[4] = (ushort)(idxStart + 3);
			indices[5] = (ushort)(idxStart + 2);
			cmd.Buffer.Elements.SetData(indices);
		}
		static Action<RenderState> _resetDelegate = ResetState;
		static void ResetState(RenderState rs)
		{
			rs.Cull = true;
		}

		public void End()
		{
			vbo.SetData(vertices, vertexCount);
		}
	}
}

