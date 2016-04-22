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

