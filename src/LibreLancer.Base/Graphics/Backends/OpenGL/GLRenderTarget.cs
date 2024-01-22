namespace LibreLancer.Graphics.Backends.OpenGL;

abstract class GLRenderTarget : IRenderTarget
{
    internal abstract void BindFramebuffer();
    public abstract void Dispose();
}
