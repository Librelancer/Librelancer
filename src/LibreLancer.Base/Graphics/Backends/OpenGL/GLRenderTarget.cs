namespace LibreLancer.Graphics.Backends.OpenGL;

internal abstract class GLRenderTarget : IRenderTarget
{
    public abstract bool HasStencil { get; }
    internal abstract void BindFramebuffer();
    public abstract void Dispose();
}
