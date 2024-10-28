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

    public void BlitToBuffer(RenderTarget2D other, Point offset)
    {
    }

    public void BlitToScreen(Point offset)
    {
    }

    public int Width { get; }
    public int Height { get; }
}
