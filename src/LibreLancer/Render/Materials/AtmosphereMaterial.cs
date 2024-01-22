// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials
{
	public class AtmosphereMaterial : RenderMaterial
	{
		public Color4 Ac = Color4.White;
		public Color4 Dc = Color4.White;
		public string DtSampler;
		public SamplerFlags DtFlags;
		public Vector3 CameraPosition;
		public float Alpha;
		public float Fade; //TODO: This is unimplemented in shader. Higher values seem to make the effect more intense?
		public float Scale;

        public AtmosphereMaterial(ResourceManager library) : base(library) { }



		public override unsafe void Use (RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
		{
			rstate.DepthEnabled = true;
			rstate.BlendMode = BlendMode.Normal;
            var sh = Shaders.Atmosphere.Get(rstate, rstate.HasFeature(GraphicsFeature.GLES) ? ShaderFeatures.VERTEX_LIGHTING : 0);
			sh.SetAc(Ac);
			sh.SetDc(Dc);
			sh.SetOc(Alpha);
			sh.SetTileRate(Fade);
            var w = Matrix4x4.CreateScale(Scale) * World.Source[0];
            sh.SetDtSampler(0);
            if (GetTexture(0, DtSampler) == null)
                sh.SetOc(0);
			BindTexture(rstate, 0, DtSampler, 0, DtFlags);
			var normalmat = w;
            Matrix4x4.Invert(normalmat, out normalmat);
            normalmat = Matrix4x4.Transpose(normalmat);
			SetLights(sh, ref lights, rstate.FrameNumber);
            sh.SetWorld(ref w, ref normalmat);
            rstate.Shader = sh;
        }

		public override void ApplyDepthPrepass(RenderContext rstate)
		{
			throw new InvalidOperationException();
		}

		public override bool IsTransparent
		{
			get
			{
				return true;
			}
		}
	}
}

