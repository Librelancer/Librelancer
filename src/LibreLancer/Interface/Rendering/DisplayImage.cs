// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;

namespace LibreLancer.Interface
{
    [UiLoadable]
    public class DisplayImage : DisplayElement
    {
        public InterfaceImage Image { get; set; }
        public InterfaceColor Tint { get; set; }
        private Texture2D texture;
        
        public override void Render(UiContext context, RectangleF clientRectangle)
        {
            if (Image == null) return;
            if (!CanRender(context)) return;
            context.Mode2D();
            var color = (Tint ?? InterfaceColor.White).GetColor(context.GlobalTime);
            var rect = context.PointsToPixels(clientRectangle);
            if (Image.Type == InterfaceImageKind.Triangle)
            {
                var x = rect.X;
                var y = rect.Y;
                var w = rect.Width;
                var h = rect.Height;
                var dc = Image.DisplayCoords;
                var pa = new Vector2(x + dc.X0 * w, y + dc.Y0 * h);
                var pb = new Vector2(x + dc.X1 * w, y + dc.Y1 * h);
                var pc = new Vector2(x + dc.X2 * w, y + dc.Y2 * h);
                context.Renderer2D.DrawTriangle(texture, pa, pb, pc,
                    new Vector2(Image.TexCoords.X0, 1 - Image.TexCoords.Y0),
                    new Vector2(Image.TexCoords.X1, 1 - Image.TexCoords.Y1),
                    new Vector2(Image.TexCoords.X2, 1 - Image.TexCoords.Y2), 
                    color
                );
            }
            else
            {
                context.Renderer2D.DrawImageStretched(texture, rect, color, Image.Flip);
            }
        }

        bool CanRender(UiContext context)
        {
            if (texture != null) return true;
            texture = context.Data.ResourceManager.FindTexture(Image.TexName) as Texture2D;
            if (texture == null) return false;
            return true;
        }
    }
}