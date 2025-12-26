using LibreLancer.Graphics;

namespace LibreLancer;

public interface IGLWindow
{
    RenderContext RenderContext { get; }
    bool IsUiThread();
}
