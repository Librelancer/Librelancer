// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
namespace LibreLancer
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

		ShaderVariables GetShader(IVertexType vertextype)
		{
			if (vertextype is VertexPositionNormalTexture)
			{
				return ShaderCache.Get(
					"Atmosphere.vs",
					"AtmosphereMaterial_PositionTexture.frag"
				);
			}
			throw new NotImplementedException ();
		}

		public override void Use (RenderState rstate, IVertexType vertextype, ref Lighting lights)
		{
			rstate.DepthEnabled = true;
			rstate.BlendMode = BlendMode.Normal;
			var sh = GetShader (vertextype);
			sh.SetAc(Ac);
			sh.SetDc(Dc);
			sh.SetOc(Alpha);
			sh.SetTileRate(Fade);
			sh.SetWorld(ref World);
			sh.SetView(Camera);
			sh.SetViewProjection(Camera);
			sh.SetDtSampler(0);
			BindTexture(rstate, 0, DtSampler, 0, DtFlags);
			var normalmat = World;
			normalmat.Invert();
			normalmat.Normalize();
			SetLights(sh, ref lights);
			sh.SetNormalMatrix(ref normalmat);
			sh.UseProgram ();
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
	}
}

