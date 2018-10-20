// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
namespace LibreLancer
{
	public class DetailMap2Dm1Msk2PassMaterial : RenderMaterial
	{
		public Color4 Ac = Color4.White;
		public Color4 Dc = Color4.White;
		public string DtSampler;
		public SamplerFlags DtFlags;
		public string Dm1Sampler;
		public SamplerFlags Dm1Flags;
		public int FlipU;
		public int FlipV;
		public float TileRate;

		public DetailMap2Dm1Msk2PassMaterial ()
		{
		}
        static ShaderVariables sh_posNormalTexture;
		static ShaderVariables GetShader(IVertexType vertextype)
		{
			if (vertextype is VertexPositionNormalTexture) {
                if(sh_posNormalTexture == null)
				sh_posNormalTexture = ShaderCache.Get (
					"PositionTextureFlip.vs",
					"DetailMap2Dm1Msk2PassMaterial.frag"
				);
                return sh_posNormalTexture;
			}
			throw new NotImplementedException ();
		}
		public override void Use (RenderState rstate, IVertexType vertextype, ref Lighting lights)
		{
			rstate.DepthEnabled = true;
			rstate.BlendMode = BlendMode.Opaque;

			ShaderVariables sh = GetShader (vertextype);
			sh.SetWorld (ref World);
			sh.SetViewProjection (Camera);
			sh.SetView (Camera);
			sh.SetAc(Ac);
			sh.SetDc(Dc);
			sh.SetTileRate(TileRate);
			sh.SetFlipU(FlipU);
			sh.SetFlipV(FlipV);

			sh.SetDtSampler(0);
			BindTexture (rstate ,0, DtSampler, 0, DtFlags);
			sh.SetDm1Sampler(1);
			BindTexture (rstate, 1, Dm1Sampler, 1, Dm1Flags);
			SetLights(sh, ref lights);
			var normalMatrix = World;
			normalMatrix.Invert();
			normalMatrix.Transpose();
			sh.SetNormalMatrix(ref normalMatrix);
			sh.UseProgram ();
		}

		public override void ApplyDepthPrepass(RenderState rstate)
		{
			rstate.BlendMode = BlendMode.Normal;
			NormalPrepassShader.SetWorld(ref World);
			NormalPrepassShader.SetViewProjection(Camera);
			NormalPrepassShader.UseProgram();
		}

		public override bool IsTransparent
		{
			get
			{
				return false;
			}
		}
	}
}

