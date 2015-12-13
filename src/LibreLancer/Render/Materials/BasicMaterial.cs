using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using LibreLancer.Vertices;
namespace LibreLancer
{
	public class BasicMaterial : RenderMaterial
	{
		public string Type;

		public Color4 Dc = Color4.White;
		public Texture DtSampler;
		public float Oc = 0f;
		public bool AlphaEnabled = false;
		public Color4 Ec = new Color4(0,0,0,0);
		public Texture EtSampler;

		public BasicMaterial (string type)
		{
			Type = type;
		}

		Shader GetShader(IVertexType vertextype)
		{
			var vert = vertextype.GetType().Name;
			switch (vert)
			{
			case "VertexPositionNormalTexture":
				return ShaderCache.Get(
					"Basic_PositionNormalTexture.vs", 
					"Basic_PositionNormalTexture.frag"
				);
			case "VertexPositionNormalTextureTwo":
				return ShaderCache.Get(
					"Basic_PositionNormalTextureTwo.vs", 
					"Basic_PositionNormalTextureTwo.frag"
				);
			case "VertexPositionColorTexture":
				return ShaderCache.Get (
					"Basic_PositionColorTexture.vs",
					"Basic_PositionColorTexture.frag"
				);
			case "VertexPositionTexture":
				return ShaderCache.Get (
					"Basic_PositionTexture.vs",
					"Basic_PositionColorTexture.frag"
				);
			default:
				throw new NotImplementedException(vert);
			}
		}

		public override void Use (IVertexType vertextype, Lighting lights)
		{
			GL.Enable (EnableCap.DepthTest);
			var shader = GetShader (vertextype);
			shader.SetMatrix ("World", ref World);
			shader.SetMatrix ("ViewProjection", ref ViewProjection);
			//Dt
			shader.SetInteger ("DtSampler", 0);
			BindTexture (DtSampler, TextureUnit.Texture0, false);
			//Dc
			shader.SetColor4("Dc", Dc);
			//Oc
			shader.SetFloat("Oc", Oc);
			if (AlphaEnabled) {
				GL.Enable (EnableCap.Blend);
				GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			} else {
				GL.Disable (EnableCap.Blend);
			}
			//Ec
			shader.SetColor4("Ec", Ec);
			//EtSampler
			shader.SetInteger("EtSampler", 1);
			BindTexture (EtSampler, TextureUnit.Texture1, false);
			//Set lights
			SetLights(shader, lights);

			shader.UseProgram ();
		}
	}
}

