using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using LibreLancer.Vertices;
namespace LibreLancer
{
	public abstract class RenderMaterial
	{
		public Matrix4 World = Matrix4.Identity;
		public Matrix4 ViewProjection = Matrix4.Identity;
		public abstract void Use (RenderState rstate, IVertexType vertextype, Lighting lights);
		static Texture2D nullTexture;

		protected void SetLights(Shader shader, Lighting lights)
		{
			shader.SetColor4("AmbientColor", lights.Ambient);
			shader.SetInteger ("LightCount", lights.Lights.Count);
			for (int i = 0; i < lights.Lights.Count; i++) {
				var lt = lights.Lights [i];
				shader.SetVector3 ("LightsPos", lt.Position, i);
				shader.SetVector3 ("LightsRot", lt.Rotation, i);
				shader.SetColor4 ("LightsColor", lt.Color, i);
				shader.SetInteger ("LightsRange", lt.Range, i);
				shader.SetVector3 ("LightsAttenuation", lt.Attenuation, i);
			}
		}
		protected void BindTexture(Texture tex, TextureUnit unit, bool throwonNull = true)
		{
			if (tex == null) {
				if (throwonNull)
					throw new Exception ();
				if (nullTexture == null) {
					nullTexture = new Texture2D (256, 256, false, SurfaceFormat.Color);
					Color4b[] colors = new Color4b[nullTexture.Width * nullTexture.Height];
					for (int i = 0; i < colors.Length; i++)
						colors [i] = Color4b.White;
					nullTexture.SetData<Color4b> (colors);
				}
				nullTexture.BindTo (unit);
			} else
				tex.BindTo (unit);
		}
	}
}

