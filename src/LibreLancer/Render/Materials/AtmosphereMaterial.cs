using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
namespace LibreLancer
{
	public class AtmosphereMaterial : RenderMaterial
	{
		public Color4 Ac = Color4.White;
		public Color4 Dc = Color4.White;
		public Texture DtSampler;
		public SamplerFlags DtFlags;
		public Vector3 CameraPosition;
		public float Alpha;
		public float Fade;
		public float Scale;

		Shader GetShader(IVertexType vertextype)
		{
			switch (vertextype.GetType ().Name) {
			case "VertexPositionTexture":
				return ShaderCache.Get (
					"Atmosphere.vs",
					"AtmosphereMaterial_PositionTexture.frag"
				);
			default:
				throw new NotImplementedException ();
			}
		}
		public override void Use (RenderState rstate, IVertexType vertextype, Lighting lights)
		{

			rstate.BlendMode = BlendMode.Normal;
			var sh = GetShader (vertextype);

			sh.SetColor4 ("Ac", Ac);
			sh.SetColor4 ("Dc", Dc);
			sh.SetVector3 ("CameraPosition", CameraPosition);
			sh.SetFloat ("Alpha", Alpha);
			sh.SetFloat ("Fade", Fade);
			sh.SetFloat ("Scale", Scale);
			sh.SetMatrix ("World", ref World);
			sh.SetMatrix ("ViewProjection", ref ViewProjection);

			sh.SetInteger ("DtSampler", 0);
			BindTexture (DtSampler, TextureUnit.Texture0, DtFlags);

			sh.UseProgram ();
		}
	}
}

