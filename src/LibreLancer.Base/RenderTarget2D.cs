// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	public class RenderTarget2D : Texture2D
	{
		public uint FBO;
		public DepthBuffer DepthBuffer;
		public static void ClearBinding()
		{
			GL.BindFramebuffer(GL.GL_FRAMEBUFFER, 0);
		}
		public RenderTarget2D (int width, int height) : base( width, height)
		{
			//generate the FBO
			FBO = GL.GenFramebuffer ();
			GL.BindFramebuffer (GL.GL_FRAMEBUFFER, FBO);
			//make the depth buffer
			DepthBuffer = new DepthBuffer(width, height);
			GL.FramebufferRenderbuffer (GL.GL_FRAMEBUFFER, 
				GL.GL_DEPTH_ATTACHMENT, 
				GL.GL_RENDERBUFFER, DepthBuffer.ID);
			//bind the texture
			GL.FramebufferTexture2D (GL.GL_FRAMEBUFFER, 
				GL.GL_COLOR_ATTACHMENT0, 
				GL.GL_TEXTURE_2D, ID, 0);
			//unbind the FBO
			GL.BindFramebuffer (GL.GL_FRAMEBUFFER, 0);
		}
		public void BindFramebuffer()
		{
			GL.BindFramebuffer(GL.GL_FRAMEBUFFER, FBO);
		}
		public override void Dispose ()
		{
			GL.DeleteFramebuffer (FBO);
			DepthBuffer.Dispose();
			base.Dispose ();
		}
	}
}

