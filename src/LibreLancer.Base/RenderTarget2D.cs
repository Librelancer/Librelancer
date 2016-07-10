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

namespace LibreLancer
{
	public class RenderTarget2D : Texture2D
	{
		public uint FBO;
		uint depthbuffer;
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
			depthbuffer = GL.GenRenderbuffer ();
			GL.BindRenderbuffer (GL.GL_RENDERBUFFER, depthbuffer);
			GL.RenderbufferStorage (GL.GL_RENDERBUFFER, GL.GL_DEPTH_COMPONENT24, width, height);
			GL.FramebufferRenderbuffer (GL.GL_FRAMEBUFFER, 
				GL.GL_DEPTH_ATTACHMENT, 
				GL.GL_RENDERBUFFER, depthbuffer);
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
			GL.DeleteRenderbuffer (depthbuffer);
			base.Dispose ();
		}
	}
}

