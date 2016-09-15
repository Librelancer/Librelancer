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
		const int MAX_BILLBOARDS = 40000;

		Shader shader;
		VertexPositionColorTexture[] vertices;
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
			vertices = new VertexPositionColorTexture[MAX_BILLBOARDS * 4];
			rendat = new RenderData[MAX_BILLBOARDS];
			vbo = new VertexBuffer(typeof(VertexPositionColorTexture), MAX_BILLBOARDS * 4, true);
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
        public Shader GetShader(string shader)
        {
            return ShaderCache.Get(
                "Billboard.vs",
                shader
            );
        }

		public void DrawCustomShader(
			Shader shader,
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
				shader,
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
			CreateBillboard(
				position,
				size,
				color,
				angle,
				topleft,
				topright,
				bottomleft,
				bottomright,
				camera.View.GetRight(),
				camera.View.GetUp()
			);

		}

		void CreateBillboard(Vector3 position, Vector2 size, Color4 color, float angle, Vector2 topleft, Vector2 topright, Vector2 bottomleft, Vector2 bottomright, Vector3 src_right, Vector3 src_up)
		{
			var s = (float)Math.Sin(angle);
			var c = (float)Math.Cos(angle);
			var up = c * src_right - s * src_up;
			var right = s * src_right + c * src_up;

			var sz1 = size * -0.5f;
			var sz2 = size * new Vector2(0.5f, -0.5f);
			var sz3 = size * new Vector2(-0.5f, 0.5f);
			var sz4 = size * 0.5f;
			vertices[vertexCount++] = new VertexPositionColorTexture()
			{
				Position = position + (right * sz1.X) + (up * sz1.Y),
				Color = color,
				TextureCoordinate = bottomleft
			};
			vertices[vertexCount++] = new VertexPositionColorTexture()
			{
				Position = position + (right * sz2.X) + (up * sz2.Y),
				Color = color,
				TextureCoordinate = topleft
			};
			vertices[vertexCount++] = new VertexPositionColorTexture()
			{
				Position = position + (right * sz3.X) + (up * sz3.Y),
				Color = color,
				TextureCoordinate = bottomright
			};
			vertices[vertexCount++] = new VertexPositionColorTexture()
			{
				Position = position + (right * sz4.X) + (up * sz4.Y),
				Color = color,
				TextureCoordinate = topright
			};
		}

		public void DrawPerspective(
			Texture2D texture,
			Vector3 Position,
			Vector2 size,
			Color4 color,
			Vector2 topleft,
			Vector2 topright,
			Vector2 bottomleft,
			Vector2 bottomright,
			Vector3 normal,
			float angle,
			int layer,
			BlendMode blend = BlendMode.Normal
		)
		{
			var right = Vector3.Cross(normal, Vector3.UnitY).Normalized();

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
				bottomright,
				right,
				Vector3.UnitY
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
				shader.SetView(ref v);
				shader.SetViewProjection(ref vp);
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
			_frameStart = true;
		}
	}
}

