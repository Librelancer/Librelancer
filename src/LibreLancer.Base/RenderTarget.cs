// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;

namespace LibreLancer
{
    public abstract class RenderTarget : IDisposable
    {
        internal abstract void BindFramebuffer();
        public abstract void Dispose();
        
        internal static void ClearBinding()
        {
            GL.BindFramebuffer(GL.GL_FRAMEBUFFER, 0);
        }
    }
}