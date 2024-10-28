// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Graphics.Backends.OpenGL
{
    class GLRenderTarget2D : GLRenderTarget, IRenderTarget2D
	{
		public uint FBO;
        private GLRenderContext context;

        private GLTexture2D texture;

        public int Width => texture.Width;
        public int Height => texture.Height;

        public GLRenderTarget2D (GLRenderContext context, GLTexture2D texture, GLDepthBuffer depth)
        {
            this.context = context;
            this.texture = texture;
			//generate the FBO
			FBO = GL.GenFramebuffer ();
			GL.BindFramebuffer (GL.GL_FRAMEBUFFER, FBO);
			//make the depth buffer
			GL.FramebufferRenderbuffer (GL.GL_FRAMEBUFFER,
				GL.GL_DEPTH_ATTACHMENT,
				GL.GL_RENDERBUFFER, depth.ID);
			//bind the texture
			GL.FramebufferTexture2D (GL.GL_FRAMEBUFFER,
				GL.GL_COLOR_ATTACHMENT0,
				GL.GL_TEXTURE_2D, texture.ID, 0);
			//unbind the FBO
			GL.BindFramebuffer (GL.GL_FRAMEBUFFER, 0);
		}

		internal override void BindFramebuffer()
		{
			GL.BindFramebuffer(GL.GL_FRAMEBUFFER, FBO);
		}

        public void BlitToScreen(Point offset)
        {
            RenderContext.Instance.Renderer2D.Flush();
            RenderContext.Instance.ApplyViewport();
            RenderContext.Instance.ApplyScissor();
            context.PrepareBlit(true);
            GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, FBO);
            GL.BindFramebuffer(GL.GL_DRAW_FRAMEBUFFER, 0);
            GL.BlitFramebuffer(0, 0, texture.Width, texture.Height, offset.X, offset.Y, offset.X + texture.Width, offset.Y + texture.Height,
                GL.GL_COLOR_BUFFER_BIT, GL.GL_LINEAR);
            GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, 0);
        }

        public void BlitToBuffer(RenderTarget2D other, Point offset)
        {
            RenderContext.Instance.Renderer2D.Flush();
            RenderContext.Instance.ApplyViewport();
            RenderContext.Instance.ApplyScissor();
            context.PrepareBlit(true);
            var Y = ((GLRenderTarget2D)other.Backing).texture.Height;
            GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, FBO);
            GL.BindFramebuffer(GL.GL_DRAW_FRAMEBUFFER, ((GLRenderTarget2D)other.Backing).FBO);
            GL.BlitFramebuffer(0, 0, texture.Width, texture.Height, offset.X, Y - offset.Y, offset.X + texture.Width, Y - (offset.Y + texture.Height),
                GL.GL_COLOR_BUFFER_BIT, GL.GL_LINEAR);
            GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, 0);
        }

        public void BlitToScreen()
        {
            RenderContext.Instance.Renderer2D.Flush();
            context.PrepareBlit(false);
            GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, FBO);
            GL.BindFramebuffer(GL.GL_DRAW_FRAMEBUFFER, 0);
            GL.BlitFramebuffer(0, 0, texture.Width, texture.Height, 0, 0, texture.Width, texture.Height,
                GL.GL_COLOR_BUFFER_BIT, GL.GL_LINEAR);
            GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, 0);
        }

        public override void Dispose ()
		{
			GL.DeleteFramebuffer(FBO);
        }
	}
}

