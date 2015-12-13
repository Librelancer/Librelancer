using System;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
namespace LibreLancer
{
	public class Masked2DetailMapMaterial : RenderMaterial
	{
		public Color4 Ac = Color4.White;
		public Color4 Dc = Color4.White;
		public Texture DtSampler;
		public Texture Dm0Sampler;
		public Texture Dm1Sampler;
		public float TileRate0;
		public float TileRate1;
		public int FlipU;
		public int FlipV;

		Shader GetShader(IVertexType vertextype)
		{
			if (vertextype.GetType ().Name == "VertexPositionTexture") {
				return ShaderCache.Get (
					"PositionTextureFlip.vs",
					"Masked2DetailMapMaterial.frag"
				);
			} else {
				throw new NotImplementedException ();
			}
		}

		public override void Use (IVertexType vertextype, Lighting lights)
		{
			var sh = GetShader (vertextype);

			sh.SetColor4 ("Ac", Ac);
			sh.SetColor4 ("Dc", Dc);
			sh.SetFloat ("TileRate0", TileRate0);
			sh.SetFloat ("TileRate1", TileRate1);
			sh.SetInteger ("FlipU", FlipU);
			sh.SetInteger ("FlipV", FlipV);

			sh.SetInteger ("DtSampler", 0);
			BindTexture (DtSampler, TextureUnit.Texture0);
			sh.SetInteger ("Dm0Sampler", 1);
			BindTexture (Dm0Sampler, TextureUnit.Texture1);
			sh.SetInteger ("Dm1Sampler", 2);
			BindTexture (Dm1Sampler, TextureUnit.Texture2);

			sh.UseProgram ();
		}
	}
}

