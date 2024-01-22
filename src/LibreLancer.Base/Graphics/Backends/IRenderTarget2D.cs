namespace LibreLancer.Graphics.Backends;

interface IRenderTarget2D : IRenderTarget
{
    void BlitToScreen();
    int Width { get; }
    int Height { get; }
}
