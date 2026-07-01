using LibreLancer.Graphics;

namespace LibreLancer.Interface;

// Used to integrate with SpaceGameplay, higher performance
public class CallbackWidget : UiWidget
{
    public delegate void RenderEvent(UiContext context, double delta, DrawList2D drawList, RectangleF clientRectangle);

    public event RenderEvent? OnRender;
    public override void Render(UiContext context, double delta, DrawList2D drawList)
    {
        OnRender?.Invoke(context, delta, drawList, ClientRectangle);
    }
}
