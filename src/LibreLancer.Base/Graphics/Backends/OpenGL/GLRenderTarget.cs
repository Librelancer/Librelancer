namespace LibreLancer.Graphics.Backends.OpenGL;

internal abstract class GLRenderTarget : IRenderTarget
{
    internal abstract void BindFramebuffer();
    public abstract void Dispose();
}
