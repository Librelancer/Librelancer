// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
namespace LibreLancer
{
	public class DetailMapMaterial : RenderMaterial
	{
		public string DmSampler;
		public SamplerFlags DmFlags;
		public float TileRate;
		public int FlipU;
		public int FlipV;
		public Color4 Ac;
		public Color4 Dc;
		public string DtSampler;
		public SamplerFlags DtFlags;

        static ShaderVariables sh_posNormalTexture;
		static ShaderVariables GetShader(IVertexType vertextype) {
			if (vertextype is VertexPositionNormalTexture) {
                if(sh_posNormalTexture == null)
				return ShaderCache.Get (
					"PositionTextureFlip.vs",
					"DetailMapMaterial.frag"
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
            sh.SetView(Camera);
            sh.SetViewProjection(Camera);

			sh.SetAc(Ac);
			sh.SetDc(Dc);
			sh.SetTileRate(TileRate);
			sh.SetFlipU(FlipU);
			sh.SetFlipV(FlipV);

			sh.SetDtSampler(0);
			BindTexture (rstate, 0, DtSampler, 0, DtFlags);
			sh.SetDmSampler(1);
			BindTexture (rstate, 1, DmSampler, 1, DmFlags);
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

