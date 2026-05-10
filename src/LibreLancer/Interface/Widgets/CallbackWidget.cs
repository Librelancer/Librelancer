using LibreLancer.Graphics;

namespace LibreLancer.Interface;

// Used to integrate with SpaceGameplay, higher performance
public class CallbackWidget : UiWidget
{
    public delegate void RenderEvent(UiContext context, DrawList2D drawList, RectangleF parentRectangle);

    public event RenderEvent? OnRender;
    public override void Render(UiContext context, DrawList2D drawList, RectangleF parentRectangle)
    {
        OnRender?.Invoke(context, drawList, parentRectangle);
    }
}
