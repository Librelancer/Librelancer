/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;

namespace LibreLancer
{
	public abstract class RenderMaterial
	{
		public Matrix4 World = Matrix4.Identity;
		public Matrix4 ViewProjection = Matrix4.Identity;
		public ILibFile Library;
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
		protected void BindTexture(string tex, TextureUnit unit, SamplerFlags flags, bool throwonNull = true)
		{
			if (tex == null)
			{
				if (throwonNull)
					throw new Exception();
				if (nullTexture == null)
				{
					nullTexture = new Texture2D(256, 256, false, SurfaceFormat.Color);
					Color4b[] colors = new Color4b[nullTexture.Width * nullTexture.Height];
					for (int i = 0; i < colors.Length; i++)
						colors[i] = Color4b.White;
					nullTexture.SetData<Color4b>(colors);
				}
				nullTexture.BindTo(unit);
			}
			else
			{
				var t = Library.FindTexture(tex);
				((Texture2D)t).BindTo(unit);
			}
			if ((flags & SamplerFlags.ClampToEdgeU) == SamplerFlags.ClampToEdgeU) {
				GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToEdge);
			}
			if ((flags & SamplerFlags.ClampToEdgeV) == SamplerFlags.ClampToEdgeV) {
				GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToEdge);
			}
			if ((flags & SamplerFlags.MirrorRepeatU) == SamplerFlags.MirrorRepeatU) {
				GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.TextureWrapS);
			}
			if ((flags & SamplerFlags.MirrorRepeatU) == SamplerFlags.MirrorRepeatV) {
				GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.TextureWrapT);
			}
		}
	}
}

