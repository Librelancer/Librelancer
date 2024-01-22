namespace LibreLancer.Graphics.Backends.Null;

class NullRenderTarget2D : IRenderTarget2D
{
    public NullRenderTarget2D(int width, int height)
    {
        Width = width;
        Height = height;
    }
    public void Dispose()
    {
    }

    public void BlitToScreen()
    {
    }

    public int Width { get; }
    public int Height { get; }
}
