using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using LibreLancer.Vertices;
namespace LibreLancer
{
	public class AtmosphereMaterial : RenderMaterial
	{
		public Color4 Ac = Color4.White;
		public Color4 Dc = Color4.White;
		public Texture DtSampler;
		public float Alpha;
		public float Fade;
		public float Scale;

		Shader GetShader(IVertexType vertextype)
		{
			switch (vertextype.GetType ().Name) {
			case "VertexPositionTexture":
				return ShaderCache.Get (
					"Basic_PositionTexture.vs",
					"AtmosphereMaterial_PositionTexture.frag"
				);
			default:
				throw new NotImplementedException ();
			}
		}
		public override void Use (RenderState rstate, IVertexType vertextype, Lighting lights)
		{
			throw new NotImplementedException ();
			var sh = GetShader (vertextype);

			sh.SetColor4 ("Ac", Ac);
			sh.SetColor4 ("Dc", Dc);
			sh.SetFloat ("Alpha", Alpha);
			sh.SetFloat ("Fade", Fade);
			sh.SetFloat ("Scale", Scale);
			sh.SetMatrix ("World", ref World);
			sh.SetMatrix ("ViewProjection", ref ViewProjection);

			sh.SetInteger ("DtSampler", 0);
			BindTexture (DtSampler, TextureUnit.Texture0);

			sh.UseProgram ();
		}
	}
}

