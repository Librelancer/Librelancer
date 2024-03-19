// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Graphics;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class DisplayImage : DisplayElement
    {
        public InterfaceImage Image { get; set; }
        public InterfaceColor Tint { get; set; }

        public float Angle { get; set; }
        public float ScaleX { get; set; } = 1;
        public float ScaleY { get; set; } = 1;
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }

        public bool OneInvSrcColor { get; set; } = false;

        private Texture2D texture;

        public override void Render(UiContext context, RectangleF clientRectangle)
        {
            if (!Enabled || Image == null) return;
            if (!CanRender(context)) return;
            var color = (Tint ?? InterfaceColor.White).GetColor(context.GlobalTime);
            var blendMode = OneInvSrcColor ? BlendMode.OneInvSrcColor : BlendMode.Normal;
            clientRectangle.Width *= ScaleX;
            clientRectangle.Height *= ScaleY;
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
                context.RenderContext.Renderer2D.DrawTriangle(texture, pa, pb, pc,
                    new Vector2(Image.TexCoords.X0, 1 - Image.TexCoords.Y0),
                    new Vector2(Image.TexCoords.X1, 1 - Image.TexCoords.Y1),
                    new Vector2(Image.TexCoords.X2, 1 - Image.TexCoords.Y2),
                    color
                );
            }
            else
            {
                var animX = (float)(context.GlobalTime * (Image.AnimU * (Image.TexCoords.X3 - Image.TexCoords.X0)));
                var animY = (float)(context.GlobalTime * (Image.AnimV * (Image.TexCoords.Y3 - Image.TexCoords.Y0)));
                var anim = new Vector2(animX, animY);
                var src = new TexSource(
                    new Vector2(Image.TexCoords.X0, Image.TexCoords.Y0) + anim,
                    new Vector2(Image.TexCoords.X0 + Image.TexCoords.X3, Image.TexCoords.Y0) + anim,
                    new Vector2(Image.TexCoords.X0, Image.TexCoords.Y0 + Image.TexCoords.Y3) + anim,
                    new Vector2(Image.TexCoords.X0 + Image.TexCoords.X3, Image.TexCoords.Y0 + Image.TexCoords.Y3) + anim
                );
                var a = Angle + Image.Angle;
                if (Math.Abs(a) > float.Epsilon)
                {
                    var oX = context.PointsToPixels(OffsetX);
                    var oY = context.PointsToPixels(OffsetY);
                    var cos = MathF.Cos(Angle);
                    var sin = MathF.Sin(Angle);
                    var px = oX * cos - oY * sin;
                    var py = oX * sin + oY * cos;
                    rect.X += (int)px;
                    rect.Y += (int)py;
                    context.RenderContext.Renderer2D.DrawRotated(
                        texture, src, rect, new Vector2(Image.OriginX * rect.Width, Image.OriginY * rect.Height), color, blendMode,
                        a, Image.Flip, Image.Rotation);
                }
                else
                {
                    rect.X += context.PointsToPixels(OffsetX);
                    clientRectangle.Y += context.PointsToPixels(OffsetY);
                    context.RenderContext.Renderer2D.Draw(texture, src, rect, color, blendMode, Image.Flip,
                        Image.Rotation);
                }
            }
        }

        bool CanRender(UiContext context)
        {
            if (texture != null) {
                if(texture.IsDisposed) texture = context.Data.ResourceManager.FindTexture(Image.TexName) as Texture2D;
                return texture != null;
            }
            texture = context.Data.ResourceManager.FindTexture(Image.TexName) as Texture2D;
            if (texture == null) return false;
            return true;
        }
    }
}
