// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Graphics.Text;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    public abstract class UiWidget : IDisposable
    {
        public string ID { get; set;  }
        public string ClassName { get; set; }
        public AnchorKind Anchor { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public UiRenderable Background { get; set; }
        public UiRenderable Border { get; set; }

        public bool Visible { get; set; } = true;
        public bool Enabled { get; set; } = true;
        //Style resolution code
        protected static T Cascade<T>(T? style, T? style2, T self) where T : struct
        {
            if (!IsDefault(self)) return self;
            if (CheckValue(style2)) return style2.Value;
            if (CheckValue(style)) return style.Value;
            return default(T);
        }
        static bool CheckValue<T>(T? value) where T : struct => !(value is null) && !IsDefault(value.Value);
        static bool IsDefault<T>(T value) => EqualityComparer<T>.Default.Equals(value, default(T));
        protected static T Cascade<T>(T style, T style2, T self) where T : class => (self ?? style2 ?? style);

        static TextAlignment CastAlign(HorizontalAlignment h)
        {
            if (h == HorizontalAlignment.Center) return TextAlignment.Center;
            if (h == HorizontalAlignment.Right) return TextAlignment.Right;
            return TextAlignment.Left;
        }
        protected void DrawText(
            UiContext context,
            ref CachedRenderString cache,
            RectangleF myRectangle,
            float textSize,
            string font,
            InterfaceColor textColor,
            InterfaceColor shadowColor,
            HorizontalAlignment horizontalAlign,
            VerticalAlignment verticalAlign,
            bool clip,
            string text,
            float alpha = 1f,
            bool wrap = false
         )
        {
            if (string.IsNullOrEmpty(text)) return;
            if (myRectangle.Width <= 1 || myRectangle.Height <= 1) return;
            if (string.IsNullOrEmpty(font)) font = "$Normal";
            if (textSize <= 0) textSize = 10;
            var color = (textColor ?? InterfaceColor.White).GetColor(context.GlobalTime);
            color.A *= alpha;
            if (color.A < float.Epsilon) return;
            var fnt = context.Data.GetFont(font);
            var size = context.TextSize(textSize);
            var lineHeight = context.RenderContext.Renderer2D.LineHeight(fnt, size);
            var drawRect = context.PointsToPixels(myRectangle);
            var sz = context.RenderContext.Renderer2D.MeasureStringCached(ref cache, fnt, size, text, false, CastAlign(horizontalAlign),
                wrap ? drawRect.Width : 0);
            //workaround for font substitution causing layout issues - e.g. CJK
            //TODO: How to get max lineheight of fonts in string?
            if (sz.Y > lineHeight && sz.Y < (lineHeight * 2)) lineHeight = sz.Y;
            float drawX, drawY;
            if (!wrap)
            {
                switch (horizontalAlign)
                {
                    case HorizontalAlignment.Left:
                        drawX = drawRect.X;
                        break;
                    case HorizontalAlignment.Right:
                    {
                        drawX = drawRect.X + drawRect.Width - sz.X;
                        break;
                    }
                    default: // Center
                    {
                        drawX = drawRect.X + (drawRect.Width / 2f) - (sz.X / 2f);
                        break;
                    }
                }
            }
            else
            {
                drawX = drawRect.X;
            }
            switch (verticalAlign) {
                case VerticalAlignment.Top:
                    drawY = drawRect.Y;
                    break;
                case VerticalAlignment.Bottom:
                    drawY = drawRect.Y + drawRect.Height - lineHeight;
                    break;
                default: //Center
                    drawY = drawRect.Y + (drawRect.Height / 2) - lineHeight / 2;
                    break;
            }
            var shadow = new OptionalColor();
            if (shadowColor != null) {
                shadow = new OptionalColor(shadowColor.Color);
                shadow.Color.A *= alpha;
            }
            if (clip) {
                context.RenderContext.ScissorEnabled = true;
                context.RenderContext.ScissorRectangle = drawRect;
            }
            context.RenderContext.Renderer2D.DrawStringCached(ref cache, fnt, size, text, drawX, drawY, color, false, shadow, CastAlign(horizontalAlign),
                wrap ? drawRect.Width : 0);
            if (clip) {
                context.RenderContext.ScissorEnabled = false;
            }
        }
        public abstract void Render(UiContext context, RectangleF parentRectangle);

        private Stylesheet _lastSheet;
        public virtual void ApplyStylesheet(Stylesheet sheet)
        {
            _lastSheet = sheet;
        }

        public void ReloadStyle()
        {
            if(_lastSheet != null) ApplyStylesheet(_lastSheet);
        }
        public virtual void UnFocus()
        {
        }

        protected UiAnimation CurrentAnimation;
        private float aspectRatio = 1;
        protected void Update(UiContext context, Vector2 myPos)
        {
            aspectRatio = context.ViewportWidth / context.ViewportHeight;
            double delta = context.DeltaTime;
            callback?.Invoke(delta);
            if (CurrentAnimation != null) {
                CurrentAnimation.SetWidgetPosition(myPos);
                CurrentAnimation.Update(delta, aspectRatio);
                if (!CurrentAnimation.Running)
                {
                    if (CurrentAnimation.FinalPositionSet.HasValue)
                    {
                        animSetPos = CurrentAnimation.FinalPositionSet.Value;
                    }
                    CurrentAnimation = null;
                }
            }
        }

        private event Action<double> callback;
        public void OnUpdate(Closure handler)
        {
            callback += (x) =>
            {
                handler.Call(x);
            };
        }

        private event Action escapePressed;
        public void OnEscape(Closure handler)
        {
            escapePressed += () => handler.Call();
        }

        private Vector2? animSetPos;
        protected Vector2 AnimatedPosition(Vector2 myPos)
        {
            if (CurrentAnimation != null && CurrentAnimation.Running)
                return CurrentAnimation.CurrentPosition;
            return animSetPos ?? myPos;
        }

        public void Animate(string name, float offsetTime, float duration)
        {
            switch (name.ToLowerInvariant())
            {
                case "flyinleft":
                    var left = new FlyInLeft(Vector2.Zero, offsetTime, duration);
                    left.From = -GetDimensions().X - 10;
                    CurrentAnimation = left;
                    CurrentAnimation.Begin(aspectRatio);
                    break;
                case "flyinright":
                    var right = new FlyInRight(Vector2.Zero, offsetTime, duration);
                    CurrentAnimation = right;
                    CurrentAnimation.Begin(aspectRatio);
                    break;
                case "flyoutleft":
                    var outleft = new FlyOutLeft(Vector2.Zero, offsetTime, duration);
                    outleft.To = -GetDimensions().X - 10;
                    CurrentAnimation = outleft;
                    CurrentAnimation.Begin(aspectRatio);
                    break;
                case "flyoutright":
                    var outright = new FlyOutRight(Vector2.Zero, aspectRatio, Width, offsetTime, duration);
                    CurrentAnimation = outright;
                    CurrentAnimation.Begin(aspectRatio);
                    break;
                case "flyinbottom":
                    var inbottom = new FlyInBottom(Vector2.Zero, offsetTime, duration);
                    CurrentAnimation = inbottom;
                    CurrentAnimation.Begin(aspectRatio);
                    break;
                case "flyoutbottom":
                    var outbottom = new FlyOutBottom(Vector2.Zero, offsetTime, duration);
                    CurrentAnimation = outbottom;
                    CurrentAnimation.Begin(aspectRatio);
                    break;
            }
        }

        public override string ToString()
        {
            return $"{ID ?? "(no id)"} - {GetType()}";
        }

        public virtual bool MouseWanted(UiContext context, RectangleF parentRectangle, float x, float y)
        {
            return false;
        }

        [WattleScriptHidden]
        public virtual bool WantsEscape() => Visible && escapePressed != null;

        [WattleScriptHidden]
        public virtual void OnEscapePressed()
        {
            if(Visible)
                escapePressed?.Invoke();
        }

        public virtual void OnMouseDown(UiContext context, RectangleF parentRectangle) { }

        public virtual void OnMouseClick(UiContext context, RectangleF parentRectangle) {}

        public virtual void OnMouseDoubleClick(UiContext context, RectangleF parentRectangle) { }

        public virtual void OnMouseWheel(UiContext context, RectangleF parentRectangle, float delta) { }

        public virtual void OnMouseUp(UiContext context, RectangleF parentRectangle) { }
        public virtual void OnKeyDown(UiContext context, Keys key, bool control) { }
        public virtual void OnTextInput(string text) { }
        public virtual Vector2 GetDimensions() => new Vector2(Width, Height);
        public virtual UiWidget GetElement(string elementID)
        {
            if (string.IsNullOrWhiteSpace(elementID)) return null;
            if (elementID.Equals(ID, StringComparison.OrdinalIgnoreCase)) return this;
            return null;
        }
        public virtual void Dispose()  { }
    }
}
