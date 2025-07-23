// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Resources;
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
		public float Alpha;
		public float Fade; //TODO: This is unimplemented in shader. Higher values seem to make the effect more intense?
		public float Scale;

        public AtmosphereMaterial(ResourceManager library) : base(library) { }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct AtmosphereParameters
        {
            public Color4 Dc;
            public Color4 Ac;
            public float Oc;
            public float Fade;
        }


		public override unsafe void Use (RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
		{
            var sh = AllShaders.Atmosphere.Get(rstate.HasFeature(GraphicsFeature.GLES) ? 1U : 0U);
            SetWorld(sh);
            var p = new AtmosphereParameters() { Dc = Dc, Ac = Ac, Fade = Fade, Oc = Alpha };
            if (GetTexture(0, DtSampler) == null)
                p.Oc = 0;
            sh.SetUniformBlock(3, ref p);
			BindTexture(rstate, 0, DtSampler, 0, DtFlags);
            var w = Matrix4x4.CreateScale(Scale) * World.Source[0];
			var normalmat = w;
            Matrix4x4.Invert(normalmat, out normalmat);
            normalmat = Matrix4x4.Transpose(normalmat);
			SetLights(sh, ref lights, rstate.FrameNumber);
            SetWorld(sh, w, normalmat);

            rstate.DepthEnabled = true;
            rstate.BlendMode = BlendMode.Normal;
            rstate.Shader = sh;
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

