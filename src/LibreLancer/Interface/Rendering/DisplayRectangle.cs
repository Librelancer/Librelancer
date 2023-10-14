using LibreLancer;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class DisplayRectangle : DisplayElement
    {
        public float MarginTop { get; set; }
        public float MarginBottom { get; set; }
        public float MarginLeft { get; set; }
        public float MarginRight { get; set; }
        public float Width { get; set; } = 1;
        public int WidthPx { get; set; }
        public InterfaceColor Color { get; set; }

        public override void Render(UiContext context, RectangleF clientRectangle)
        {
            if(!Enabled) return;
            var color = (Color ?? InterfaceColor.White).GetColor(context.GlobalTime);
            var withMargins = new RectangleF(
                clientRectangle.X + MarginLeft,
                clientRectangle.Y + MarginTop,
                clientRectangle.Width - MarginLeft - MarginRight,
                clientRectangle.Height - MarginTop - MarginBottom
            );
            var rect = context.PointsToPixels(withMargins);
            var width = WidthPx > 0 ? WidthPx : context.PointsToPixels(Width);
            context.RenderContext.Renderer2D.DrawRectangle(rect, color, width);
        }
    }
}
