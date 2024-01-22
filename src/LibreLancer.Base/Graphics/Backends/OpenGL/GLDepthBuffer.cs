// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Graphics.Backends.OpenGL
{
    class GLDepthBuffer : IDepthBuffer
    {
        internal uint ID;

        public GLDepthBuffer(int width, int height)
        {
            ID = GL.GenRenderbuffer();
            GL.BindRenderbuffer(GL.GL_RENDERBUFFER, ID);
            GL.RenderbufferStorage(GL.GL_RENDERBUFFER, GL.GLES ? GL.GL_DEPTH_COMPONENT16 : GL.GL_DEPTH_COMPONENT24, width, height);
        }
        public void Dispose()
        {
            GL.DeleteRenderbuffer(ID);
        }
    }
}
