
using LibreLancer.Graphics;
using System.Runtime.CompilerServices;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class DisplayWireBorder : DisplayElement
    {
        public float Width { get; set; } = 1;
        public InterfaceColor? Color { get; set; }

        public override void Render(UiContext context, DrawList2D drawList, RectangleF clientRectangle, float alpha)
        {
            if(!Enabled) return;
            var color = (Color ?? InterfaceColor.White).GetColor(context.GlobalTime);
            color.A *= alpha;
            if (context.PointsToPixels(Width) <= 1) {
                drawList.DrawRectangle(context.PointsToPixels(clientRectangle), color, 1);
            }
            float w = Width / 3;
            // Left
            LR(context, drawList, clientRectangle, 0, w, 0, color);
            LR(context, drawList, clientRectangle, w, w, 1, color);
            LR(context, drawList, clientRectangle, 2 * w, w, 2, color);
            // Right
            var rW = clientRectangle.Width - Width;
            LR(context, drawList, clientRectangle, rW, w, 0, color);
            LR(context, drawList, clientRectangle, rW + w, w, 1, color);
            LR(context, drawList, clientRectangle, rW + 2 * w, w, 2, color);
            // Top
            TB(context, drawList, clientRectangle, 0, w, 0, color);
            TB(context, drawList, clientRectangle, w, w, 1, color);
            TB(context, drawList, clientRectangle, 2 * w, w, 2, color);
            // Bottom
            var rH = clientRectangle.Height - Width;
            TB(context, drawList, clientRectangle, rH, w, 0, color);
            TB(context, drawList, clientRectangle, rH + w, w, 1, color);
            TB(context, drawList, clientRectangle, rH + 2 * w, w, 2, color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LR(UiContext context, DrawList2D drawList, RectangleF client, float pos, float w, int alphaSide, Color4 color) {
            var r = new RectangleF(client.X + pos, client.Y + w * 1.5f, w, client.Height - 3f * w);
            var alphaZero = new Color4(color.R, color.G, color.B, 0);
            Color4 left = alphaSide == 0 ? alphaZero : color;
            Color4 right = alphaSide == 2 ? alphaZero : color;
            drawList.FillRectangleColors(context.PointsToPixelsF(r), left, right, left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TB(UiContext context, DrawList2D drawList, RectangleF client, float pos, float w, int alphaSide, Color4 color) {
            var r = new RectangleF(client.X + 1.5f * w, client.Y + pos, client.Width - 3f * w, w);
            var alphaZero = new Color4(color.R, color.G, color.B, 0);
            Color4 top = alphaSide == 0 ? alphaZero : color;
            Color4 bottom = alphaSide == 2 ? alphaZero : color;
            drawList.FillRectangleColors(context.PointsToPixelsF(r), top, top, bottom, bottom);
        }
    }
}
