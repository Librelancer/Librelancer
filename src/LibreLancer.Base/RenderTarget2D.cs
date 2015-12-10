using System;
using OpenTK.Graphics.OpenGL;
namespace LibreLancer
{
	public class RenderTarget2D : Texture2D
	{
		public int FBO;
		int depthbuffer;
		public RenderTarget2D (int width, int height) : base( width, height)
		{
			//generate the FBO
			FBO = GL.GenFramebuffer ();
			GL.BindFramebuffer (FramebufferTarget.Framebuffer, FBO);
			//make the depth buffer
			depthbuffer = GL.GenRenderbuffer ();
			GL.BindRenderbuffer (RenderbufferTarget.Renderbuffer, depthbuffer);
			GL.RenderbufferStorage (RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);
			GL.FramebufferRenderbuffer (FramebufferTarget.Framebuffer, 
				FramebufferAttachment.DepthAttachment, 
				RenderbufferTarget.Renderbuffer, depthbuffer);
			//bind the texture
			GL.FramebufferTexture2D (FramebufferTarget.Framebuffer, 
				FramebufferAttachment.ColorAttachment0, 
				TextureTarget.Texture2D, ID, 0);
			//unbind the FBO
			GL.BindFramebuffer (FramebufferTarget.Framebuffer, 0);
		}
		public override void Dispose ()
		{
			GL.DeleteFramebuffer (FBO);
			GL.DeleteRenderbuffer (depthbuffer);
			base.Dispose ();
		}
	}
}

