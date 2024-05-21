// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;

namespace LibreLancer.Render
{
	public unsafe class Billboards
	{
		const int MAX_BILLBOARDS = 10000;

        RenderData[] rendat;
        //Basic
        Shaders.ShaderVariables shaderBasic;
        BillboardVert* verticesBasic;
        VertexBuffer vboBasic;
        ElementBuffer iboBasic;
        ushort[] indicesBasic = new ushort[MAX_BILLBOARDS * 6];

		public Billboards(RenderContext context)
        {
            shaderBasic = Shaders.Billboard.Get(context);
			shaderBasic.Shader.SetInteger(shaderBasic.Shader.GetLocation("tex0"), 0);
			rendat = new RenderData[MAX_BILLBOARDS];
			vboBasic = new VertexBuffer(context, typeof(BillboardVert), MAX_BILLBOARDS * 4, true);
			iboBasic = new ElementBuffer(context, MAX_BILLBOARDS * 6, true);
			vboBasic.SetElementBuffer(iboBasic);
		}

		[StructLayout(LayoutKind.Sequential)]
		struct BillboardVert : IVertexType
		{
			public Vector3 Position;
			public Color4 Color;
			public Vector2 TextureCoordinate;
			public Vector3 Dimensions;
			public VertexDeclaration GetVertexDeclaration()
			{
				return new VertexDeclaration (
					sizeof(float) * 3 + sizeof(float) * 4 + sizeof(float) * 2 + sizeof(float) * 3,
					new VertexElement (VertexSlots.Position, 3, VertexElementType.Float, false, 0),
					new VertexElement (VertexSlots.Color, 4, VertexElementType.Float, false, sizeof(float) * 3),
					new VertexElement (VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 7),
					new VertexElement (VertexSlots.Dimensions, 3, VertexElementType.Float, false, sizeof(float) * 9)
				);
			}
		}

        public ushort GetBlendMode(int index)
        {
            return rendat[index].BlendMode;
        }
        public uint GetTextureID(int index)
        {
            return rendat[index].Texture.ID;
        }
		struct RenderData
		{
			public Texture Texture;
			public ushort BlendMode;
			public ushort IndexStart;
            public byte Triangle;

            public RenderData(Texture tex, ushort blend, ushort idxStart)
            {
                Texture = tex;
                BlendMode = blend;
                IndexStart = idxStart;
                Triangle = 0;
            }
            public static RenderData CreateTri(Texture tex, ushort blend, ushort idxStart)
			{
                return new RenderData(tex, blend, idxStart) { Triangle = 1 };
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
		int billboardCount = 0;
		CommandBuffer buffer;
		public void Begin(ICamera cam, CommandBuffer cmd)
		{
			camera = cam;
			billboardCount = vertexCountBasic = indexCountBasic = 0;
			buffer = cmd;
            verticesBasic = (BillboardVert*)vboBasic.BeginStreaming();
		}

		/* Some pre-calculated values */
		const float cos120 = -0.5000001f;
		const float sin120 = 0.8660254f;
		const float cos240 = -0.4999999f;
		const float sin240 = -0.8660254f;
		const float deg30 = -0.5235988f;

		void CreateTriBillboard(Vector3 position, float radius, Color4 color, float angle, Vector2 texA, Vector2 texB, Vector2 texC)
		{
			/* Create triangle points */
			var rOn2 = radius * 0.5f; //this should be much faster than division
			var rOn4 = radius * 0.25f;
			var ptC = new Vector3 (0, -(rOn2), angle);
			var ptB = new Vector3 (-rOn2 * sin120, -rOn2 * cos120, angle);
			var ptA = new Vector3 (-rOn4 * sin240, -rOn4 * cos240, angle); //triangle is half as tall as it is wide

			verticesBasic [vertexCountBasic++] = new BillboardVert ()
			{
				Position = position,
				Color = color,
				TextureCoordinate = texA,
				Dimensions = ptA
			};

			verticesBasic [vertexCountBasic++] = new BillboardVert ()
			{
				Position = position,
				Color = color,
				TextureCoordinate = texB,
				Dimensions = ptB
			};

			verticesBasic [vertexCountBasic++] = new BillboardVert ()
			{
				Position = position,
				Color = color,
				TextureCoordinate = texC,
				Dimensions = ptC
			};
		}

		void CreateBillboard(Vector3 position, Vector2 size, Color4 color, float angle, Vector2 topleft, Vector2 topright, Vector2 bottomleft, Vector2 bottomright)
		{
			var sz1 = new Vector3 (size.X * -0.5f, size.Y * -0.5f, angle);
			var sz2 = new Vector3(size.X * 0.5f, size.Y * -0.5f, angle);
			var sz3 = new Vector3(size.X * -0.5f, size.Y * 0.5f, angle);
			var sz4 = new Vector3 (size.X * 0.5f, size.Y * 0.5f, angle);
			verticesBasic[vertexCountBasic++] = new BillboardVert()
			{
				Position = position,
				Color = color,
				TextureCoordinate = bottomleft,
				Dimensions = sz1
			};
			verticesBasic[vertexCountBasic++] = new BillboardVert()
			{
				Position = position,
				Color = color,
				TextureCoordinate = topleft,
				Dimensions = sz2
			};
			verticesBasic[vertexCountBasic++] = new BillboardVert()
			{
				Position = position,
				Color = color,
				TextureCoordinate = bottomright,
				Dimensions = sz3
			};
			verticesBasic[vertexCountBasic++] = new BillboardVert()
			{
				Position = position,
				Color = color,
				TextureCoordinate = topright,
				Dimensions = sz4
			};
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
			ushort blend = BlendMode.Normal
		)
		{
			rendat[billboardCount] = RenderData.CreateTri(
				texture,
				blend,
				(ushort)vertexCountBasic
			);
			CreateTriBillboard(
				Position,
				radius,
				color,
				angle,
				texa,
				texb,
				texc
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
			ushort blend = BlendMode.Normal
		)
		{
			rendat[billboardCount] = new RenderData(
				texture,
				blend,
				(ushort)vertexCountBasic
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

        int lastDatHash = -1;
		int datindex = 0;
        int lastIndexBasic = 0;
        //
        int indexCountBasic = 0;
        int fillCountBasic = 0;
        int vertexCountBasic = 0;

        public void AddIndices(int index)
        {
            var dat = rendat[index];
            var idxStart = dat.IndexStart;
            indicesBasic[fillCountBasic++] = idxStart;
            indicesBasic[fillCountBasic++] = (ushort)(idxStart + 1);
            indicesBasic[fillCountBasic++] = (ushort)(idxStart + 2);
            if (dat.Triangle != 1)
            {
                indicesBasic[fillCountBasic++] = (ushort)(idxStart + 1);
                indicesBasic[fillCountBasic++] = (ushort)(idxStart + 3);
                indicesBasic[fillCountBasic++] = (ushort)(idxStart + 2);
            }
            _iboFilled = false;
        }

        bool _iboFilled = false;
		public void Render(int index, int hash, RenderContext rs)
		{
			if (hash != lastDatHash && lastDatHash != -1)
				FlushCommands(rs);
			lastDatHash = hash;
			datindex = index;
            var dat = rendat[index];
            indexCountBasic += dat.Triangle == 1 ? 3 : 6;
        }



		public void FillIbo()
		{
            if (!_iboFilled)
            {
                iboBasic.SetData(indicesBasic, fillCountBasic);
                _iboFilled = true;
                fillCountBasic = 0;
            }
		}

        void DrawCommands(RenderContext rs, int start, int count)
        {
            rs.Shader = shaderBasic;
            rs.Cull = false;

            rs.BlendMode = rendat[datindex].BlendMode;
            if (_frameStart)
            {
                var v = camera.View;
                var vp = camera.ViewProjection;
                _frameStart = false;
            }

            rendat[datindex].Texture.BindTo(0);
            vboBasic.Draw(PrimitiveTypes.TriangleList, 0, start, count / 3);
            rs.Cull = true;
            lastDatHash = -1;
        }

        bool _frameStart = true;
		public void FlushCommands(RenderContext rs)
		{
            FillIbo();
			if (indexCountBasic == 0)
			{
				lastDatHash = -1;
				return;
			}
            DrawCommands(rs, lastIndexBasic, indexCountBasic);
            lastIndexBasic += indexCountBasic;
            indexCountBasic = 0;
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

		public void End()
		{
            vboBasic.EndStreaming(vertexCountBasic);
			_frameStart = true;
            _iboFilled = false;
            lastIndexBasic = fillCountBasic;
		}
    }
}
