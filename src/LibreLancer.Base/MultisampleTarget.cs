// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
	public class MultisampleTarget : IDisposable
	{
		uint texID;
		uint fbo;
		uint depthID;
		public int Width { get; private set; }
		public int Height { get; private set; }

		public MultisampleTarget(int width, int height, int samples)
		{
			Width = width;
			Height = height;
			texID = GL.GenTexture();
			GLBind.BindTexture(0, GL.GL_TEXTURE_2D_MULTISAMPLE, texID);
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

		public void Bind()
		{
			GL.BindFramebuffer(GL.GL_FRAMEBUFFER, fbo);
			GL.Enable(GL.GL_MULTISAMPLE);
		}
		public void BlitToScreen()
		{
			GL.Disable(GL.GL_MULTISAMPLE);
			//Unbind everything
			GL.BindFramebuffer(GL.GL_FRAMEBUFFER, 0);
			//read from our fbo
			GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, fbo);
			//draw to the back buffer
			GL.BindFramebuffer(GL.GL_DRAW_FRAMEBUFFER, 0);
			GL.DrawBuffer(GL.GL_BACK);
			//blit
			GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, GL.GL_COLOR_BUFFER_BIT, GL.GL_NEAREST);
		}

		public void Dispose()
		{
			RenderTarget2D.ClearBinding();
			GL.DeleteFramebuffer(fbo);
			GL.DeleteRenderbuffer(depthID);
			GL.DeleteTexture(texID);
		}
	}
}
