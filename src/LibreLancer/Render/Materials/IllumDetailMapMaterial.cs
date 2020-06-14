// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;

namespace LibreLancer
{
	public class IllumDetailMapMaterial : RenderMaterial
	{
		public Color4 Ac = Color4.White;
		public Color4 Dc = Color4.White;
		public string DtSampler;
		public SamplerFlags DtFlags;
		public string Dm0Sampler;
		public SamplerFlags Dm0Flags;
		public string Dm1Sampler;
		public SamplerFlags Dm1Flags;
		public float TileRate0;
		public float TileRate1;
		public int FlipU;
		public int FlipV;


		public override void Use(RenderState rstate, IVertexType vertextype, ref Lighting lights)
		{
			rstate.DepthEnabled = true;
			rstate.BlendMode = BlendMode.Opaque;

            var sh = Shaders.IllumDetailMapMaterial.Get();
            sh.SetViewProjection(Camera);
			sh.SetWorld(World);
            sh.SetView(Camera);

			sh.SetAc(Ac);
			sh.SetDc(Dc);
			sh.SetTileRate0(TileRate0);
			sh.SetTileRate1(TileRate1);
			sh.SetFlipU(FlipU);
			sh.SetFlipV(FlipV);

			sh.SetDtSampler(0);
			BindTexture(rstate, 0, DtSampler, 0, DtFlags);
			sh.SetDm0Sampler(1);
			BindTexture(rstate, 1, Dm0Sampler, 1, Dm0Flags);
			sh.SetDm1Sampler(2);
			BindTexture(rstate, 2, Dm1Sampler, 2, Dm1Flags);
			SetLights(sh, ref lights, Camera.FrameNumber);
            sh.UseProgram();
		}

		public override void ApplyDepthPrepass(RenderState rstate)
		{
			rstate.BlendMode = BlendMode.Normal;
            var sh = Shaders.DepthPass_Normal.Get();
			sh.SetWorld(World);
			sh.SetViewProjection(Camera);
			sh.UseProgram();
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

