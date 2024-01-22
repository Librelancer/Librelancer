namespace LibreLancer.Graphics.Backends;

interface IMultisampleTarget : IRenderTarget
{
    int Width { get; }
    int Height { get; }
    void BlitToScreen();
    void BlitToRenderTarget(IRenderTarget2D rTarget);
}
