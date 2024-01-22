// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials
{
	public class NomadMaterial : RenderMaterial
	{
		public Color4 Dc = Color4.White;
		public string DtSampler;
		public SamplerFlags DtFlags;

		public string BtSampler;
		public SamplerFlags BtFlags;

		public string NtSampler;
		public SamplerFlags NtFlags;

		public float Oc = 1f;

        public NomadMaterial(ResourceManager library) : base(library) { }



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

        public override bool DisableCull => true;

        public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
		{
			rstate.BlendMode = BlendMode.Normal;
            var shader = Shaders.Nomad.Get(rstate);
			shader.SetWorld(World);
            //Colors
			shader.SetDc(Dc);
			//Dt
			shader.SetDtSampler(0);
			BindTexture(rstate, 0, DtSampler, 0, DtFlags);
			//Nt
			shader.SetDmSampler(1); //Repurpose DmSampler
			BindTexture(rstate, 1, NtSampler ?? "NomadRGB1_NomadAlpha1", 1, NtFlags);
			//Bt

			//Disable MaterialAnim
			shader.SetMaterialAnim(new Vector4(0, 0, 1, 1));
            rstate.Shader = shader;
        }
	}
}
