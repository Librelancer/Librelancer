// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer;

namespace LibreLancer.Interface
{
    public abstract class UiWidget : IDisposable
    {
        public string ID { get; set;  }
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

        protected void DrawText(
            UiContext context, 
            RectangleF myRectangle, 
            float textSize, 
            string font, 
            InterfaceColor textColor,
            InterfaceColor shadowColor,
            HorizontalAlignment horizontalAlign,
            VerticalAlignment verticalAlign,
            bool clip,
            string text
         )
        {
            if (string.IsNullOrEmpty(text)) return;
            if (string.IsNullOrEmpty(font)) font = "$Normal";
            if (textSize <= 0) textSize = 10;
            var color = (textColor ?? InterfaceColor.White).GetColor(context.GlobalTime);
            if (color.A < float.Epsilon) return;
            context.Mode2D();
            var fnt = context.GetFont(font);
            var size = context.TextSize(textSize);
            var lineHeight = context.Renderer2D.LineHeight(fnt, size);
            var drawRect = context.PointsToPixels(myRectangle);
            float drawX, drawY;
            switch (horizontalAlign) {
                case HorizontalAlignment.Left:
                    drawX = drawRect.X;
                    break;
                case HorizontalAlignment.Right:
                {
                    var sz = context.Renderer2D.MeasureString(fnt, size, text);
                    drawX = drawRect.X + drawRect.Width - sz.X;
                    break;
                }
                default: // Center
                {
                    var sz = context.Renderer2D.MeasureString(fnt, size, text);
                    drawX = drawRect.X + (drawRect.Width / 2f) - (sz.X / 2f);
                    break;
                }
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

            var shadow = new TextShadow();
            if (shadowColor != null) shadow = new TextShadow(shadowColor.Color);
            if (clip)
            {
                context.Renderer2D.DrawWithClip(drawRect,
                    () =>
                    {
                        context.Renderer2D.DrawStringBaseline(fnt, size, text, drawX, drawY, drawX, color, false, shadow);
                    });
            }
            else
            {
                context.Renderer2D.DrawStringBaseline(fnt, size, text, drawX, drawY, drawX, color, false, shadow);
            }
        }
        public abstract void Render(UiContext context, RectangleF parentRectangle);

        public virtual void ApplyStylesheet(Stylesheet sheet)
        {
            
        }

        protected UiAnimation CurrentAnimation;
        private TimeSpan lastTime = TimeSpan.FromSeconds(0);
        protected void Update(UiContext context, Vector2 myPos)
        {
            TimeSpan delta;
            if (lastTime == TimeSpan.FromSeconds(0))
                delta = TimeSpan.FromSeconds(0);
            else
                delta = context.GlobalTime - lastTime;
            lastTime = context.GlobalTime;
            if (CurrentAnimation != null) {
                CurrentAnimation.SetWidgetPosition(myPos);
                CurrentAnimation.Update(delta.TotalSeconds);
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
                    CurrentAnimation.Begin();
                    break;
                case "flyoutleft":
                    var outleft = new FlyOutLeft(Vector2.Zero, offsetTime, duration);
                    outleft.To = -GetDimensions().X - 10;
                    CurrentAnimation = outleft;
                    CurrentAnimation.Begin();
                    break;
            }
        }
        
        public virtual void ScriptedEvent(string ev, params object[] param) { }
        public virtual void OnMouseDown(UiContext context, RectangleF parentRectangle) { }
        public virtual void OnMouseClick(UiContext context, RectangleF parentRectangle) { }
        public virtual void OnMouseUp(UiContext context, RectangleF parentRectangle) { }
        public virtual void OnKeyDown(Keys key) { }
        public virtual void OnTextInput(string text) { }
        public virtual Vector2 GetDimensions() => new Vector2(Width, Height);
        public virtual UiWidget GetElement(string elementID)
        {
            if (string.IsNullOrWhiteSpace(elementID)) return null;
            if (elementID.Equals(ID, StringComparison.OrdinalIgnoreCase)) return this;
            return null;
        }
        public virtual void EnableScripting(UiContext context, string modalData) { }
        public virtual void Dispose()  { }
    }
}