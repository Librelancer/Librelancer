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

		public override bool IsTransparent
		{
			get
			{
				return true;
			}
		}

		public override void Use(RenderState rstate, IVertexType vertextype, Lighting lights)
		{
			rstate.BlendMode = BlendMode.Normal;
			var shader = GetShader(vertextype);
			shader.SetWorld(ref World);
			shader.SetView(Camera);
			shader.SetViewProjection(Camera);
			//Colors
			shader.SetDc(Dc);
			shader.SetOc(Oc);
			//Dt
			shader.SetDtSampler(0);
			BindTexture(rstate, 0, DtSampler, 0, DtFlags);
			//Bt
			shader.SetEtSampler(1);
			BindTexture(rstate, 1, BtSampler, 1, BtFlags);
			//Disable MaterialAnim
			shader.SetMaterialAnim(new Vector4(0, 0, 1, 1));
			shader.UseProgram();
		}
	}
}
