// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;

namespace LibreLancer.Graphics.Backends.OpenGL
{
    class GLDepthMap : GLTexture2D, IDepthMap
    {
        uint FBO;
        public GLDepthMap(int width, int height) : base(width, height, false, SurfaceFormat.Depth)
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
