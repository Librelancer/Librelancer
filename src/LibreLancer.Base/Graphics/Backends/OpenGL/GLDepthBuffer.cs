// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Graphics.Backends.OpenGL;

internal class GLDepthBuffer : IDepthBuffer
{
    internal uint ID;
    public bool HasStencil { get; private set; }

    public GLDepthBuffer(int width, int height, bool stencil)
    {
        ID = GL.GenRenderbuffer();
        GL.BindRenderbuffer(GL.GL_RENDERBUFFER, ID);
        GL.RenderbufferStorage(GL.GL_RENDERBUFFER, stencil ? GL.GL_DEPTH24_STENCIL8 : GL.GL_DEPTH_COMPONENT24, width, height);
        HasStencil = stencil;
    }
    public void Dispose()
    {
        GL.DeleteRenderbuffer(ID);
    }
}
