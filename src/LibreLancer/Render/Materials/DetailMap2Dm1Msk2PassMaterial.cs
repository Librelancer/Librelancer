// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials
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

		public DetailMap2Dm1Msk2PassMaterial (ResourceManager library) : base(library) { }

        public override void Use (RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
		{
			rstate.DepthEnabled = true;
			rstate.BlendMode = BlendMode.Opaque;

			var sh = Shaders.DetailMap2Dm1Msk2PassMaterial.Get (rstate, rstate.HasFeature(GraphicsFeature.GLES) ? ShaderFeatures.VERTEX_LIGHTING : 0);
			sh.SetWorld (World);
            sh.SetAc(Ac);
			sh.SetDc(Dc);
			sh.SetTileRate(TileRate);
			sh.SetFlipU(FlipU);
			sh.SetFlipV(FlipV);
            sh.SetDtSampler(0);
			BindTexture (rstate ,0, DtSampler, 0, DtFlags);
			sh.SetDm1Sampler(1);
            BindTexture(rstate, 1, Dm1Sampler, 1, Dm1Flags, ResourceManager.GreyTextureName);
			SetLights(sh, ref lights, rstate.FrameNumber);
            rstate.Shader = sh;
        }

		public override void ApplyDepthPrepass(RenderContext rstate)
		{
			rstate.BlendMode = BlendMode.Normal;
            var sh = Shaders.DepthPass_Normal.Get(rstate);
            sh.SetWorld(World);
            rstate.Shader = sh;
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

