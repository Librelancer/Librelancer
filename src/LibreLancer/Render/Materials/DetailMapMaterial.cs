// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials
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

        public DetailMapMaterial(ResourceManager library) : base(library) { }



		public override void Use (RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
		{
			rstate.DepthEnabled = true;
			rstate.BlendMode = BlendMode.Opaque;

            var sh = Shaders.DetailMapMaterial.Get(rstate, rstate.HasFeature(GraphicsFeature.GLES)? ShaderFeatures.VERTEX_LIGHTING : 0);
			sh.SetWorld (World);

            sh.SetAc(Ac);
			sh.SetDc(Dc);
			sh.SetTileRate(TileRate);
			sh.SetFlipU(FlipU);
			sh.SetFlipV(FlipV);

			sh.SetDtSampler(0);
			BindTexture (rstate, 0, DtSampler, 0, DtFlags);
			sh.SetDmSampler(1);
			BindTexture (rstate, 1, DmSampler, 1, DmFlags);
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

