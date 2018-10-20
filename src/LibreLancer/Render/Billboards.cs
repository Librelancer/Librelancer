// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
using LibreLancer.Vertices;
namespace LibreLancer
{
	public class Billboards
	{
		const int MAX_BILLBOARDS = 40000;

        RenderData[] rendat;
        //Basic
        ShaderVariables shaderBasic;
        BillboardVert[] verticesBasic;
        VertexBuffer vboBasic;
        ElementBuffer iboBasic;
        ushort[] indicesBasic = new ushort[MAX_BILLBOARDS * 6];
        //Rect
        ShaderVariables shaderRect;
        VertexPositionColorTexture[] verticesRect = new VertexPositionColorTexture[MAX_BILLBOARDS * 4];
        ushort[] indicesRect = new ushort[MAX_BILLBOARDS * 6];
        VertexBuffer vboRect;
        ElementBuffer iboRect;
		public Billboards()
		{
			shaderBasic = ShaderCache.Get(
				"Billboard.vs",
				"Billboard.frag"
			);
			shaderBasic.Shader.SetInteger(shaderBasic.Shader.GetLocation("tex0"), 0);
            shaderRect = ShaderCache.Get(
                "Polyline.vs",
                "Billboard.frag"
            );
            shaderRect.Shader.SetInteger(shaderRect.Shader.GetLocation("tex0"), 0);
			verticesBasic = new BillboardVert[MAX_BILLBOARDS * 4];
			rendat = new RenderData[MAX_BILLBOARDS];
			vboBasic = new VertexBuffer(typeof(BillboardVert), MAX_BILLBOARDS * 4, true);
			iboBasic = new ElementBuffer(MAX_BILLBOARDS * 6, true);
			vboBasic.SetElementBuffer(iboBasic);

            vboRect = new VertexBuffer(typeof(VertexPositionColorTexture), MAX_BILLBOARDS * 4, true);
            iboRect = new ElementBuffer(MAX_BILLBOARDS * 6, true);
            vboRect.SetElementBuffer(iboRect);
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
        enum RenderKind
        {
            Basic = 3,
            Rect = 178925648
        }
		struct RenderData
		{
			public Texture Texture;
			public BlendMode BlendMode;
            public RenderKind RenderKind;
			public ushort Index0;
			public ushort Index1;
			public ushort Index2;
			public ushort Index3;
			public ushort Index4;
			public ushort Index5;
			public RenderData(Texture tex, BlendMode blend, RenderKind kind, ushort idxStart)
			{
				Texture = tex;
				BlendMode = blend;
                RenderKind = kind;
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
                d.RenderKind = RenderKind.Basic;
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
                    hash += hash * 23 + (int)RenderKind;
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
            vertexCountRect = indexCountRect = 0;
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
			ShaderAction setup,
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
			int vc = vertexCountBasic;
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
				this,
				shader,
				setup,
				userData,
				vc,
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

			verticesBasic [vertexCountBasic++] = new BillboardVert () 
			{
				Position = position,
				Color = color,
				TextureCoordinate = texA,
				Dimensions = ptA,
				Right = src_right,
				Up = src_up
			};

			verticesBasic [vertexCountBasic++] = new BillboardVert () 
			{
				Position = position,
				Color = color,
				TextureCoordinate = texB,
				Dimensions = ptB,
				Right = src_right,
				Up = src_up
			};

			verticesBasic [vertexCountBasic++] = new BillboardVert () 
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
			verticesBasic[vertexCountBasic++] = new BillboardVert()
			{
				Position = position,
				Color = color,
				TextureCoordinate = bottomleft,
				Dimensions = sz1,
				Right = src_right,
				Up = src_up
			};
			verticesBasic[vertexCountBasic++] = new BillboardVert()
			{
				Position = position,
				Color = color,
				TextureCoordinate = topleft,
				Dimensions = sz2,
				Right = src_right,
				Up = src_up
			};
			verticesBasic[vertexCountBasic++] = new BillboardVert()
			{
				Position = position,
				Color = color,
				TextureCoordinate = bottomright,
				Dimensions = sz3,
				Right = src_right,
				Up = src_up
			};
			verticesBasic[vertexCountBasic++] = new BillboardVert()
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
			var up = normal;
			var toCamera = (camera.Position - Position).Normalized();
			var right = Vector3.Cross(toCamera, up);
			up.Normalize();
			right.Normalize();
			rendat[billboardCount] = new RenderData(
				texture,
				blend,
                RenderKind.Basic,
				(ushort)vertexCountBasic
			);
			//construct points then billboard them?
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
			Vector3 pos,
            Matrix4 world,
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
            var upref = Vector3.UnitY;
            if ((Vector3.UnitY - normal).Length < float.Epsilon)
                upref = Vector3.UnitZ;
			var srcright = Vector3.Cross(normal, upref);
			var srcup = Vector3.Cross(srcright, normal);
			srcup.Normalize();
			srcright.Normalize();
            Vector3 up, right;
            if(Math.Abs(angle) < float.Epsilon) {
                up = srcup;
                right = srcright;
            } else {
                var s = (float)Math.Sin(angle);
                var c = (float)Math.Cos(angle);
                up = c * srcright - s * srcup;
                right = s * srcright + c * srcup;
            }
			rendat[billboardCount] = new RenderData(
				texture,
				blend,
                RenderKind.Rect,
				(ushort)vertexCountRect
			);
            //
            var sz = 0.5f * size;
            verticesRect[vertexCountRect++] = new VertexPositionColorTexture(
                VectorMath.Transform(
                    pos - right * sz.X - up * sz.Y, world
                ),
                color,
                bottomleft
            );
            verticesRect[vertexCountRect++] = new VertexPositionColorTexture(
               VectorMath.Transform(
                   pos + right * sz.X - up * sz.Y, world
               ),
               color,
               topleft
           );
            verticesRect[vertexCountRect++] = new VertexPositionColorTexture(
               VectorMath.Transform(
                   pos - right * sz.X + up * sz.Y, world
               ),
               color,
               bottomright
           );
            verticesRect[vertexCountRect++] = new VertexPositionColorTexture(
               VectorMath.Transform(
                   pos + right * sz.X + up * sz.Y, world
               ),
               color,
               topright
            );
            //
			var z = RenderHelpers.GetZ(camera.Position, pos);
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
				(ushort)vertexCountBasic
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
                RenderKind.Basic,
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
        int lastIndexRect = 0;
        //
        int indexCountBasic = 0;
        int fillCountBasic = 0;
        int vertexCountBasic = 0;
        //
        int indexCountRect = 0;
        int fillCountRect = 0;
        int vertexCountRect = 0;
        public void AddIndices(int index)
        {
            var dat = rendat[index];
            if (dat.RenderKind == RenderKind.Basic)
            {
                indicesBasic[fillCountBasic++] = dat.Index0;
                indicesBasic[fillCountBasic++] = dat.Index1;
                indicesBasic[fillCountBasic++] = dat.Index2;
                if (!dat.Tri)
                {
                    indicesBasic[fillCountBasic++] = dat.Index3;
                    indicesBasic[fillCountBasic++] = dat.Index4;
                    indicesBasic[fillCountBasic++] = dat.Index5;
                }
            } else {
                indicesRect[fillCountRect++] = dat.Index0;
                indicesRect[fillCountRect++] = dat.Index1;
                indicesRect[fillCountRect++] = dat.Index2;
                if (!dat.Tri)
                {
                    indicesRect[fillCountRect++] = dat.Index3;
                    indicesRect[fillCountRect++] = dat.Index4;
                    indicesRect[fillCountRect++] = dat.Index5;
                }
            }
            _iboFilled = false;
        }

		public void AddCustomIndices(int idxStart)
		{
			indicesBasic[fillCountBasic++] = (ushort)idxStart;
			indicesBasic[fillCountBasic++] = (ushort)(idxStart + 1);
			indicesBasic[fillCountBasic++] = (ushort)(idxStart + 2);
			indicesBasic[fillCountBasic++] = (ushort)(idxStart + 1);
			indicesBasic[fillCountBasic++] = (ushort)(idxStart + 3);
			indicesBasic[fillCountBasic++] = (ushort)(idxStart + 2);
			_iboFilled = false;
		}

		public void RenderCustom(RenderState rs, Shader shdr, ShaderAction customAction, ref RenderCommand cmd)
		{
			FlushCommands(rs);
			//Setup shader default state
			rs.Cull = false;
			rs.BlendMode = BlendMode.Normal;
			var splt = new SplitInt() { I = shdr.UserTag };
			if (shdr.UserTag == 0)
			{
				splt.A = (short)shdr.GetLocation("View");
				splt.B = (short)shdr.GetLocation("ViewProjection");
				shdr.UserTag = splt.I;
			}
			var view = camera.View;
			shdr.SetMatrix(splt.A, ref view);
			var vp = camera.ViewProjection;
			shdr.SetMatrix(splt.B, ref vp);
			//User-defined
			customAction(shdr, rs, ref cmd);
			//Draw
			vboBasic.Draw(PrimitiveTypes.TriangleList, 0, lastIndexBasic, 2);
			//Set stuff
			rs.Cull = true;
			lastDatHash = -1;
			lastIndexBasic += 6;
		}

        bool _iboFilled = false;
		public void RenderStandard(int index, int hash, RenderState rs)
		{
			if (hash != lastDatHash && lastDatHash != -1)
				FlushCommands(rs);
			lastDatHash = hash;
			datindex = index;
			var dat = rendat[index];
            if (rendat[index].RenderKind == RenderKind.Basic)
                indexCountBasic += dat.Tri ? 3 : 6;
            else
                indexCountRect += dat.Tri ? 3 : 6;
		}

		public void FillIbo()
		{
            if (!_iboFilled)
            {
                iboBasic.SetData(indicesBasic, fillCountBasic);
                iboRect.SetData(indicesRect, fillCountRect);
                _iboFilled = true;
                fillCountRect = 0;
                fillCountBasic = 0;
            }
		}

		bool _frameStart = true;
		public void FlushCommands(RenderState rs)
		{
            FillIbo();
			if (indexCountBasic == 0 && indexCountRect == 0)
			{
				lastDatHash = -1;
				return;
			}
			rs.Cull = false;
			rs.BlendMode = rendat[datindex].BlendMode;
            var rect = rendat[datindex].RenderKind == RenderKind.Rect;
			if (_frameStart)
			{
				var v = camera.View;
				var vp = camera.ViewProjection;
				shaderBasic.SetView(ref v);
				shaderBasic.SetViewProjection(ref vp);
                shaderRect.SetView(ref v);
                shaderRect.SetViewProjection(ref vp);
				_frameStart = false;
			}
			rendat[datindex].Texture.BindTo(0);
            (rect ? shaderRect : shaderBasic).UseProgram();
            if(rect) {
                vboRect.Draw(PrimitiveTypes.TriangleList, 0, lastIndexRect, indexCountRect / 3);
                lastIndexRect += indexCountRect;
                indexCountRect = 0;
            } else {
                vboBasic.Draw(PrimitiveTypes.TriangleList, 0, lastIndexBasic, indexCountBasic / 3);
                lastIndexBasic += indexCountBasic;
                indexCountBasic = 0;
            }
			rs.Cull = true;
			lastDatHash = -1;
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
			vboBasic.SetData(verticesBasic, vertexCountBasic);
            vboRect.SetData(verticesRect, vertexCountRect);
			_frameStart = true;
            _iboFilled = false;
            lastIndexBasic = 0;
            lastIndexRect = 0;
		}
	}
}