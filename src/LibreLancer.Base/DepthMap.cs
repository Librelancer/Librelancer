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
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
namespace LibreLancer
{
	public class DepthMap : Texture2D	
	{
		uint FBO;
		public DepthMap(int width, int height) : base(width, height, false, SurfaceFormat.Depth)
		{
			FBO = GL.GenFramebuffer();
			SetFiltering(TextureFiltering.Nearest);
			SetWrapModeS(WrapMode.ClampToBorder);
			SetWrapModeT(WrapMode.ClampToBorder);
			Vector4 col = new Vector4(1, 1, 1, 1);
			GL.TexParameterfv(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_BORDER_COLOR, ref col);
			GL.BindFramebuffer(GL.GL_FRAMEBUFFER, FBO);
			GL.FramebufferTexture2D(GL.GL_FRAMEBUFFER, GL.GL_DEPTH_ATTACHMENT, GL.GL_TEXTURE_2D, ID, 0);
			GL.DrawBuffer(0);
			GL.ReadBuffer(0);
			GL.BindFramebuffer(GL.GL_FRAMEBUFFER, 0);
		}

		public void BindFramebuffer()
		{
			GL.BindFramebuffer(GL.GL_FRAMEBUFFER, FBO);
		}

		public override void Dispose()
		{
			base.Dispose();
			GL.DeleteFramebuffer(FBO);
		}
	}
}
