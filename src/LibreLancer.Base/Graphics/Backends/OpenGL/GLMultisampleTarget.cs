// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Graphics.Backends.OpenGL;

internal class GLMultisampleTarget : GLRenderTarget, IMultisampleTarget
{
    private uint texID;
    private uint fbo;
    private uint depthID;
    public int Width { get; private set; }
    public int Height { get; private set; }

    private GLRenderContext context;

    private uint resolveTexID;
    private uint resolveFboID;

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

    private void CreateResolveFbo()
    {
        if (resolveFboID == 0)
        {
            resolveFboID = GL.GenFramebuffer();
            GL.BindFramebuffer(GL.GL_FRAMEBUFFER, resolveFboID);
            resolveTexID = GL.GenTexture();
            GLBind.Trash();
            GLBind.BindTextureForModify(GL.GL_TEXTURE_2D, resolveTexID);
            GL.TexImage2D(GL.GL_TEXTURE_2D, 0,
                GL.GL_RGBA,
                Width, Height, 0,
                GL.GL_BGRA, GL.GL_UNSIGNED_BYTE, IntPtr.Zero);
            GL.FramebufferTexture2D (GL.GL_FRAMEBUFFER,
                GL.GL_COLOR_ATTACHMENT0,
                GL.GL_TEXTURE_2D, resolveTexID, 0);
            GL.BindFramebuffer(GL.GL_FRAMEBUFFER, 0);
        }
    }

    public void BlitToScreen(Point offset)
    {
        RenderContext.Instance.Renderer2D.Flush();
        RenderContext.Instance.ApplyViewport();
        RenderContext.Instance.ApplyScissor();
        if(!GL.GLES) GL.Disable(GL.GL_MULTISAMPLE);
        int Y = RenderContext.Instance.DrawableSize.Y;

        // GLES does not allow an msaa buffer to be blitted with an offset
        if (offset != default && GL.GLES)
        {
            context.PrepareBlit(false);
            //create 2d rt
            CreateResolveFbo();
            //unbind everything
            GL.BindFramebuffer(GL.GL_FRAMEBUFFER, 0);
            //read from our fbo
            GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, fbo);
            //draw to the rt
            GL.BindFramebuffer(GL.GL_DRAW_FRAMEBUFFER, resolveFboID);
            GL.DrawBuffer(GL.GL_COLOR_ATTACHMENT0);
            //resolve msaa
            GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, GL.GL_COLOR_BUFFER_BIT, GL.GL_NEAREST);
            //blit resolved to screen
            GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, resolveFboID);
            GL.BindFramebuffer(GL.GL_DRAW_FRAMEBUFFER, 0);
            GL.DrawBuffer(GL.GL_BACK);
            GL.BlitFramebuffer(0, Height, Width, 0, offset.X, Y - offset.Y, offset.X + Width, Y - (offset.Y + Height), GL.GL_COLOR_BUFFER_BIT, GL.GL_NEAREST);
            GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, 0);
            return;
        }
        context.PrepareBlit(false);
        //Unbind everything
        GL.BindFramebuffer(GL.GL_FRAMEBUFFER, 0);
        //read from our fbo
        GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, fbo);
        //draw to the back buffer
        GL.BindFramebuffer(GL.GL_DRAW_FRAMEBUFFER, 0);
        GL.DrawBuffer(GL.GL_BACK);
        //blit
        GL.BlitFramebuffer(0, Height, Width, 0, offset.X, Y - offset.Y, offset.X + Width, Y - (offset.Y + Height), GL.GL_COLOR_BUFFER_BIT, GL.GL_NEAREST);
        GL.BindFramebuffer(GL.GL_READ_FRAMEBUFFER, 0);
    }

    public void BlitToRenderTarget(IRenderTarget2D rTarget)
    {
        RenderContext.Instance.Renderer2D.Flush();
        if (!GL.GLES) GL.Disable(GL.GL_MULTISAMPLE);
        context.PrepareBlit(false);
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
        if (resolveTexID != 0)
        {
            GL.DeleteFramebuffer(resolveFboID);
            GL.DeleteTexture(resolveTexID);
        }
    }
}
