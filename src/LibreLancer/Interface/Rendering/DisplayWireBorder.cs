
using System.Runtime.CompilerServices;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class DisplayWireBorder : DisplayElement
    {
        public float Width { get; set; } = 1;
        public InterfaceColor Color { get; set; }

        public override void Render(UiContext context, RectangleF clientRectangle)
        {
            if(!Enabled) return;
            var color = (Color ?? InterfaceColor.White).GetColor(context.GlobalTime);
            if (context.PointsToPixels(Width) <= 1) {
                context.RenderContext.Renderer2D.DrawRectangle(context.PointsToPixels(clientRectangle), color, 1);
            }
            float w = Width / 3;
            //Left
            LR(context, clientRectangle, 0, w, 0, color);
            LR(context, clientRectangle, w, w, 1, color);
            LR(context, clientRectangle, 2 * w, w, 2, color);
            //Right
            var rW = clientRectangle.Width - Width;
            LR(context, clientRectangle, rW, w, 0, color);
            LR(context, clientRectangle, rW + w, w, 1, color);
            LR(context, clientRectangle, rW + 2 * w, w, 2, color);
            //Top
            TB(context, clientRectangle, 0, w, 0, color);
            TB(context, clientRectangle, w, w, 1, color);
            TB(context, clientRectangle, 2 * w, w, 2, color);
            //Bottom
            var rH = clientRectangle.Height - Width;
            TB(context, clientRectangle, rH, w, 0, color);
            TB(context, clientRectangle, rH + w, w, 1, color);
            TB(context, clientRectangle, rH + 2 * w, w, 2, color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void LR(UiContext context, RectangleF client, float pos, float w, int alphaSide, Color4 color) {
            var r = new RectangleF(client.X + pos, client.Y + w * 1.5f, w, client.Height - 3f * w);
            var alphaZero = new Color4(color.R, color.G, color.B, 0);
            Color4 left = alphaSide == 0 ? alphaZero : color;
            Color4 right = alphaSide == 2 ? alphaZero : color;
            context.RenderContext.Renderer2D.FillRectangleColors(context.PointsToPixelsF(r), left, right, left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void TB(UiContext context, RectangleF client, float pos, float w, int alphaSide, Color4 color) {
            var r = new RectangleF(client.X + 1.5f * w, client.Y + pos, client.Width - 3f * w, w);
            var alphaZero = new Color4(color.R, color.G, color.B, 0);
            Color4 top = alphaSide == 0 ? alphaZero : color;
            Color4 bottom = alphaSide == 2 ? alphaZero : color;
            context.RenderContext.Renderer2D.FillRectangleColors(context.PointsToPixelsF(r), top, top, bottom, bottom);
        }
    }
}
