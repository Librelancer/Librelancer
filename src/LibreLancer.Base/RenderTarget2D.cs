// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	public class RenderTarget2D : RenderTarget
	{
		public uint FBO;
		public DepthBuffer DepthBuffer { get; private set; }
		public Texture2D Texture { get; private set; }
        public int Width => Texture.Width;
        public int Height => Texture.Height;
		public RenderTarget2D (int width, int height)
        {
            Texture = new Texture2D(width, height);
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
				GL.GL_TEXTURE_2D, Texture.ID, 0);
			//unbind the FBO
			GL.BindFramebuffer (GL.GL_FRAMEBUFFER, 0);
		}
		internal override void BindFramebuffer()
		{
			GL.BindFramebuffer(GL.GL_FRAMEBUFFER, FBO);
		}

        public void BlitToScreen()
        {
            RenderContext.Instance.Renderer2D.Flush();
            if (RenderContext.Instance.applied.ScissorEnabled) {
                GL.Disable(GL.GL_SCISSOR_TEST);
                RenderContext.Instance.applied.ScissorEnabled = false;
            }
            RenderContext.Instance.applied.RenderTarget = null;
            GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, FBO);
            GL.BindFramebuffer(GL.GL_DRAW_FRAMEBUFFER, 0);
            GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, GL.GL_COLOR_BUFFER_BIT, GL.GL_LINEAR);
            GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, 0);
        }
		public override void Dispose ()
		{
			Dispose(false);
        }

        public void Dispose(bool keepTexture)
        {
            GL.DeleteFramebuffer (FBO);
            DepthBuffer.Dispose();
            if(!keepTexture)
                Texture.Dispose();
        }
	}
}

