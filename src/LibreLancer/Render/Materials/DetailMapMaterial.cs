using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
namespace LibreLancer
{
	public class DetailMapMaterial : RenderMaterial
	{
		public Texture DmSampler;
		public SamplerFlags DmFlags;
		public float TileRate;
		public int FlipU;
		public int FlipV;
		public Color4 Ac;
		public Color4 Dc;
		public Texture DtSampler;
		public SamplerFlags DtFlags;

		Shader GetShader(IVertexType vertextype) {
			if (vertextype.GetType ().Name == "VertexPositionTexture") {
				return ShaderCache.Get (
					"PositionTextureFlip.vs",
					"DetailMapMaterial.frag"
				);
			}
			throw new NotImplementedException ();
		}

		public override void Use (RenderState rstate, IVertexType vertextype, Lighting lights)
		{
			rstate.BlendMode = BlendMode.Opaque;

			Shader sh = GetShader (vertextype);
			sh.SetMatrix ("World", ref World);
			sh.SetMatrix ("ViewProjection", ref ViewProjection);

			sh.SetColor4 ("Ac", Ac);
			sh.SetColor4 ("Dc", Dc);
			sh.SetFloat ("TileRate", TileRate);
			/*if (FlipU == 1)
				sh.SetFloat ("FlipU", -1);
			else
				sh.SetFloat ("FlipU", 1);
			if (FlipV == 1)
				sh.SetFloat ("FlipV", -1);
			else
				sh.SetFloat ("FlipV", 1);*/
			sh.SetInteger ("FlipU", FlipU);
			sh.SetInteger ("FlipV", FlipV);

			sh.SetInteger ("DtSampler", 0);
			BindTexture (DtSampler, TextureUnit.Texture0, DtFlags);
			sh.SetInteger ("DmSampler", 1);
			BindTexture (DmSampler, TextureUnit.Texture1, DmFlags);

			sh.UseProgram ();
		}
	}
}

