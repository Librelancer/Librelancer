using LibreLancer;
using LibreLancer.Graphics;
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
        public float Thickness { get; set; } = 1;
        public int ThicknessPx { get; set; }
        public InterfaceColor? Color { get; set; }

        protected override void Render(UiContext context, DrawList2D drawList, RectangleF clientRectangle, Color4 tint)
        {
            if(!Enabled) return;
            var color = (Color ?? InterfaceColor.White).GetColor(context.GlobalTime) * tint;
            var withMargins = new RectangleF(
                clientRectangle.X + MarginLeft,
                clientRectangle.Y + MarginTop,
                clientRectangle.Width - MarginLeft - MarginRight,
                clientRectangle.Height - MarginTop - MarginBottom
            );
            var rect = context.PointsToPixels(withMargins);
            var thickness = ThicknessPx > 0 ? ThicknessPx : context.PointsToPixels(Thickness);
            drawList.DrawRectangle(rect, color, thickness);
        }
    }
}
