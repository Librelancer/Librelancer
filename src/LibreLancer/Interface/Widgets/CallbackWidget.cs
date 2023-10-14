namespace LibreLancer.Interface;

// Used to integrate with SpaceGameplay, higher performance
public class CallbackWidget : UiWidget
{
    public delegate void RenderEvent(UiContext context, RectangleF parentRectangle);

    public event RenderEvent OnRender;
    public override void Render(UiContext context, RectangleF parentRectangle)
    {
        OnRender?.Invoke(context, parentRectangle);
    }
}
