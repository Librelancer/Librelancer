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

		ShaderVariables shader;
		BillboardVert[] vertices;
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
			shader.Shader.SetInteger(shader.Shader.GetLocation("tex0"), 0);
			vertices = new BillboardVert[MAX_BILLBOARDS * 4];
			rendat = new RenderData[MAX_BILLBOARDS];
			vbo = new VertexBuffer(typeof(BillboardVert), MAX_BILLBOARDS * 4, true);
			ibo = new ElementBuffer(MAX_BILLBOARDS * 6, true);
			vbo.SetElementBuffer(ibo);
		}
		[StructLayout(LayoutKind.Sequential)]
		struct BillboardVert : IVertexType
		{
			public Vector3 Position;
			public Color4 Color;
			public Vector2 TextureCoordinate;
			public Vector3 Dimensions;
			public Vector3 Right;
			public Vector3 Up;
			public VertexDeclaration GetVertexDeclaration()
			{
				return new VertexDeclaration (
					sizeof(float) * 3 + sizeof(float) * 4 + sizeof(float) * 2 + sizeof(float) * 3 * 3,
					new VertexElement (VertexSlots.Position, 3, VertexElementType.Float, false, 0),
					new VertexElement (VertexSlots.Color, 4, VertexElementType.Float, false, sizeof(float) * 3),
					new VertexElement (VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 7),
					new VertexElement (VertexSlots.Dimensions, 3, VertexElementType.Float, false, sizeof(float) * 9),
					new VertexElement (VertexSlots.Right, 3, VertexElementType.Float, false, sizeof(float) * 12),
					new VertexElement (VertexSlots.Up, 3, VertexElementType.Float, false, sizeof(float) * 15)
				);
			}
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
			public static RenderData CreateTri(Texture tex, BlendMode blend, ushort idxStart)
			{
				var d = new RenderData ();
				d.Texture = tex;
				d.BlendMode = blend;
				d.Index0 = idxStart;
				d.Index1 = (ushort)(idxStart + 1);
				d.Index2 = (ushort)(idxStart + 2);
				d.Index3 = (ushort)(idxStart);
				return d;
			}
			public bool Tri {
				get {
					return Index3 == Index0;
				}
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
		public ICamera Camera
		{
			get
			{
				return camera;
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
            ).Shader;
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
			dat.Camera = camera;
			//dat.Integer = vertexCount;
			int vc = vertexCount;
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
				vc,
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
		/* Some pre-calculated values */
		const float cos120 = -0.5000001f;
		const float sin120 = 0.8660254f;
		const float cos240 = -0.4999999f;
		const float sin240 = -0.8660254f;
		const float deg30 = -0.5235988f;

		void CreateTriBillboard(Vector3 position, float radius, Color4 color, float angle, Vector2 texA, Vector2 texB, Vector2 texC, Vector3 src_right, Vector3 src_up)
		{
			/* Create triangle points */
			var rOn2 = radius * 0.5f; //this should be much faster than division
			var rOn4 = radius * 0.25f;
			var ptC = new Vector3 (0, -(rOn2), angle);
			var ptB = new Vector3 (-rOn2 * sin120, -rOn2 * cos120, angle);
			var ptA = new Vector3 (-rOn4 * sin240, -rOn4 * cos240, angle); //triangle is half as tall as it is wide

			vertices [vertexCount++] = new BillboardVert () 
			{
				Position = position,
				Color = color,
				TextureCoordinate = texA,
				Dimensions = ptA,
				Right = src_right,
				Up = src_up
			};

			vertices [vertexCount++] = new BillboardVert () 
			{
				Position = position,
				Color = color,
				TextureCoordinate = texB,
				Dimensions = ptB,
				Right = src_right,
				Up = src_up
			};

			vertices [vertexCount++] = new BillboardVert () 
			{
				Position = position,
				Color = color,
				TextureCoordinate = texC,
				Dimensions = ptC,
				Right = src_right,
				Up = src_up
			};
		}

		void CreateBillboard(Vector3 position, Vector2 size, Color4 color, float angle, Vector2 topleft, Vector2 topright, Vector2 bottomleft, Vector2 bottomright, Vector3 src_right, Vector3 src_up)
		{
			var sz1 = new Vector3 (size.X * -0.5f, size.Y * -0.5f, angle);
			var sz2 = new Vector3(size.X * 0.5f, size.Y * -0.5f, angle);
			var sz3 = new Vector3(size.X * -0.5f, size.Y * 0.5f, angle);
			var sz4 = new Vector3 (size.X * 0.5f, size.Y * 0.5f, angle);
			vertices[vertexCount++] = new BillboardVert()
			{
				Position = position,
				Color = color,
				TextureCoordinate = bottomleft,
				Dimensions = sz1,
				Right = src_right,
				Up = src_up
			};
			vertices[vertexCount++] = new BillboardVert()
			{
				Position = position,
				Color = color,
				TextureCoordinate = topleft,
				Dimensions = sz2,
				Right = src_right,
				Up = src_up
			};
			vertices[vertexCount++] = new BillboardVert()
			{
				Position = position,
				Color = color,
				TextureCoordinate = bottomright,
				Dimensions = sz3,
				Right = src_right,
				Up = src_up
			};
			vertices[vertexCount++] = new BillboardVert()
			{
				Position = position,
				Color = color,
				TextureCoordinate = topright,
				Dimensions = sz4,
				Right = src_right,
				Up = src_up
			};
		}

		public void DrawRectAppearance(
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
			//float projectedPointFactor = Vector3.Dot(camera.Position - Position, normal) / Vector3.Dot(normal, normal);
			//var projectedPoint = Position + projectedPointFactor * normal;
			var up = normal;
			var toCamera = (camera.Position - Position).Normalized();
			var right = Vector3.Cross(toCamera, up);
			//var right = camera.View.GetRight() * normal;
			//var up = camera.View.GetUp() * normal;
			//right *= normal;
			//up *= normal;
			//var up = normal;
			//var right = Camera.View.GetRight();
			up.Normalize();
			right.Normalize();

			//up.Normalize();
			//right.Normalize();
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
				up
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
			var right = Vector3.Cross(normal, Vector3.UnitY);
			var up = Vector3.Cross(right, normal);

			up.Normalize();
			right.Normalize();
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
				up
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

		public void DrawTri(
			Texture2D texture,
			Vector3 Position,
			float radius,
			Color4 color,
			Vector2 texa,
			Vector2 texb,
			Vector2 texc,
			float angle,
			int layer,
			BlendMode blend = BlendMode.Normal
		)
		{
			rendat[billboardCount] = RenderData.CreateTri(
				texture,
				blend,
				(ushort)vertexCount
			);
			CreateTriBillboard(
				Position,
				radius,
				color,
				angle,
				texa,
				texb,
				texc,
				camera.View.GetRight(),
				camera.View.GetUp()
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
        int fillCount = 0;
        int lastIndex = 0;
		int vertexCount = 0;

        public void AddIndices(int index)
        {
            var dat = rendat[index];
            indices[fillCount++] = dat.Index0;
            indices[fillCount++] = dat.Index1;
            indices[fillCount++] = dat.Index2;
			if (!dat.Tri) {
				indices [fillCount++] = dat.Index3;
				indices [fillCount++] = dat.Index4;
				indices [fillCount++] = dat.Index5;
			}
            _iboFilled = false;
        }

        bool _iboFilled = false;
		public void RenderStandard(int index, int hash, RenderState rs)
		{
			if (hash != lasthash && lasthash != -1)
				FlushCommands(rs);
			lasthash = hash;
			datindex = index;
			var dat = rendat[index];
			indexCount += dat.Tri ? 3 : 6;
			/*indices[indexCount++] = dat.Index0;
			indices[indexCount++] = dat.Index1;
			indices[indexCount++] = dat.Index2;
			indices[indexCount++] = dat.Index3;
			indices[indexCount++] = dat.Index4;
			indices[indexCount++] = dat.Index5;*/
		}

		bool _frameStart = true;
		public void FlushCommands(RenderState rs)
		{
			if (indexCount == 0)
			{
				lasthash = -1;
				return;
			}
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
            if (!_iboFilled)
            {
                ibo.SetData(indices, fillCount);
                _iboFilled = true;
                fillCount = 0;
            }
            vbo.Draw(PrimitiveTypes.TriangleList, 0, lastIndex, indexCount / 3);
			rs.Cull = true;
			lasthash = -1;
            lastIndex += indexCount;
			indexCount = 0;
		}
		[StructLayout(LayoutKind.Explicit)]
		struct SplitInt
		{
			[FieldOffset(0)]
			public short A;
			[FieldOffset(2)]
			public short B;
			[FieldOffset(0)]
			public int I;
		}
		static ShaderAction _setupDelegateCustom = SetupShaderCustom;
		static void SetupShaderCustom(Shader shdr, RenderState rs, ref RenderCommand cmd)
		{
			rs.Cull = false;
			rs.BlendMode = BlendMode.Normal;
			cmd.UserData.UserFunction(shdr,rs, ref cmd);
			var splt = new SplitInt() { I = shdr.UserTag };
			if (shdr.UserTag == 0)
			{
				splt.A = (short)shdr.GetLocation("View");
				splt.B = (short)shdr.GetLocation("ViewProjection");
				shdr.UserTag = splt.I;
			}
			shdr.SetMatrix(splt.A, ref cmd.World);
			var vp = cmd.UserData.Camera.ViewProjection;
			shdr.SetMatrix(splt.B, ref vp);
			int idxStart = cmd.Start; //re-use this
			cmd.Start = 0; //hack of the year
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
            _iboFilled = false;
            lastIndex = 0;
		}
	}

}

