// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
namespace LibreLancer
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

		public NomadMaterial()
		{
		}

		static ShaderVariables sh_one;
		static ShaderVariables sh_two;
		static ShaderVariables GetShader(IVertexType vertexType)
		{
			if (vertexType is VertexPositionNormalTextureTwo)
			{
				if (sh_two == null)
					sh_two = ShaderCache.Get("Basic_PositionNormalTextureTwo.vs", "NomadMaterial.frag");
				return sh_two;
			}
			else if (vertexType is VertexPositionNormalTexture)
			{
				if (sh_one == null)
					sh_one = ShaderCache.Get("Nomad_PositionNormalTexture.vs", "NomadMaterial.frag");
				return sh_one;
			}
			throw new NotImplementedException(vertexType.GetType().Name);
		}


		public override void ApplyDepthPrepass(RenderState rstate)
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

        public override void Use(RenderState rstate, IVertexType vertextype, ref Lighting lights)
		{
			rstate.BlendMode = BlendMode.Normal;
			var shader = GetShader(vertextype);
			shader.SetWorld(ref World);
			shader.SetView(Camera);
			shader.SetViewProjection(Camera);
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
			shader.UseProgram();
		}
	}
}
