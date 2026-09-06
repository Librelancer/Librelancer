namespace LibreLancer.Graphics.Backends.Null;

internal class NullDepthBuffer : IDepthBuffer
{
    public bool HasStencil => false;

    public void Dispose()
    {
    }
}
