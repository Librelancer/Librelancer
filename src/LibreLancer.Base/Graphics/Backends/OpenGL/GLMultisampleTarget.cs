// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


namespace LibreLancer.Graphics.Backends.OpenGL
{
    class GLMultisampleTarget : GLRenderTarget, IMultisampleTarget
	{
		uint texID;
		uint fbo;
		uint depthID;
		public int Width { get; private set; }
		public int Height { get; private set; }

        private GLRenderContext context;

		public GLMultisampleTarget(GLRenderContext context, int width, int height, int samples)
        {
            this.context = context;
			Width = width;
			Height = height;
			texID = GL.GenTexture();
			GLBind.BindTexture(0, GL.GL_TEXTURE_2D_MULTISAMPLE, texID);
            if(GL.GLES)
                GL.TexStorage2DMultisample(GL.GL_TEXTURE_2D_MULTISAMPLE, samples, GL.GL_RGBA8, width, height, true);
            else
			    GL.TexImage2DMultisample(GL.GL_TEXTURE_2D_MULTISAMPLE, samples, GL.GL_RGBA, width, height, true);
			depthID = GL.GenRenderbuffer();
			GL.BindRenderbuffer(GL.GL_RENDERBUFFER, depthID);
			GL.RenderbufferStorageMultisample(GL.GL_RENDERBUFFER, samples, GL.GL_DEPTH_COMPONENT24, width, height);
			GL.BindRenderbuffer(GL.GL_RENDERBUFFER, 0);
			fbo = GL.GenFramebuffer();
			GL.BindFramebuffer(GL.GL_FRAMEBUFFER, fbo);
			GL.FramebufferTexture2D(GL.GL_FRAMEBUFFER, GL.GL_COLOR_ATTACHMENT0, GL.GL_TEXTURE_2D_MULTISAMPLE, texID, 0);
			GL.FramebufferRenderbuffer(GL.GL_FRAMEBUFFER,
				GL.GL_DEPTH_ATTACHMENT,
				GL.GL_RENDERBUFFER, depthID);
            int status = GL.CheckFramebufferStatus(GL.GL_FRAMEBUFFER);
			GL.BindFramebuffer(GL.GL_FRAMEBUFFER, 0);
        }
        internal override void BindFramebuffer()
		{
			GL.BindFramebuffer(GL.GL_FRAMEBUFFER, fbo);
			if(!GL.GLES) GL.Enable(GL.GL_MULTISAMPLE);
		}
		public void BlitToScreen()
		{
            if(!GL.GLES) GL.Disable(GL.GL_MULTISAMPLE);
            RenderContext.Instance.Renderer2D.Flush();
            context.PrepareBlit();
            //Unbind everything
			GL.BindFramebuffer(GL.GL_FRAMEBUFFER, 0);
			//read from our fbo
			GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, fbo);
			//draw to the back buffer
			GL.BindFramebuffer(GL.GL_DRAW_FRAMEBUFFER, 0);
			GL.DrawBuffer(GL.GL_BACK);
			//blit
			GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, GL.GL_COLOR_BUFFER_BIT, GL.GL_NEAREST);
            GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, 0);
        }

        public void BlitToRenderTarget(IRenderTarget2D rTarget)
        {
            RenderContext.Instance.Renderer2D.Flush();
            if (!GL.GLES) GL.Disable(GL.GL_MULTISAMPLE);
            context.PrepareBlit();
            //Unbind everything
            GL.BindFramebuffer(GL.GL_FRAMEBUFFER, 0);
            //read from our fbo
            GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, fbo);
            //draw to the fbo
            GL.BindFramebuffer(GL.GL_DRAW_FRAMEBUFFER, ((GLRenderTarget2D)rTarget).FBO);
            GL.DrawBuffer(GL.GL_COLOR_ATTACHMENT0);
            //blit
            GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, GL.GL_COLOR_BUFFER_BIT, GL.GL_NEAREST);
            //reset state
            GL.BindFramebuffer(GL.GL_DRAW_FRAMEBUFFER, 0);
            GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, 0);
            GL.DrawBuffer(GL.GL_BACK);
        }

        public override void Dispose()
		{
            GL.DeleteFramebuffer(fbo);
			GL.DeleteRenderbuffer(depthID);
			GL.DeleteTexture(texID);
        }
	}
}
