using System.Threading.Tasks;

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

    public Task<(int Width, int Height, Bgra8[] Data)> DownloadAsync()
    {
        return Task.FromResult((Width, Height, new Bgra8[Width * Height]));
    }

    public int Width { get; }
    public int Height { get; }
}
