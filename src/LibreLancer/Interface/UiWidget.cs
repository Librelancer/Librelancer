// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Graphics.Text;
using LibreLancer.Graphics;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    public abstract class UiWidget : IDisposable
    {
        private ElementStyle? style;
        private ElementStyle resolvedStyle = new();
        protected StyledProperty<float> WidthProperty = new("Width");
        protected StyledProperty<float> HeightProperty = new("Height");
        protected StyledProperty<UiRenderable?> BackgroundProperty = new("Background");
        protected StyledProperty<UiRenderable?> BorderProperty = new("Border");
        protected bool StyleDirty = true;

        public string? ID { get; set; }
        public string? ClassName { get; set; }
        public AnchorKind Anchor { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        public ElementStyle? Style
        {
            get => style;
            set
            {
                style = value;
                OnStyleChanged();
            }
        }


        public float Width
        {
            get => WidthProperty.Value;
            set
            {
                WidthProperty.Set(value);
                OnStyleChanged();
            }
        }

        public float Height
        {
            get => HeightProperty.Value;
            set
            {
                HeightProperty.Set(value);
                OnStyleChanged();
            }
        }

        public UiRenderable? Background
        {
            get => BackgroundProperty.Value;
            set
            {
                BackgroundProperty.Set(value);
                OnStyleChanged();
            }
        }

        public UiRenderable? Border
        {
            get => BorderProperty.Value;
            set
            {
                BorderProperty.Set(value);
                OnStyleChanged();
            }
        }


        public bool Visible { get; set; } = true;

        public bool Enabled { get; set; } = true;

        public RectangleF ClientRectangle { get; protected set; }


        public void OnStyleChanged()
        {
            StyleDirty = true;
        }

        protected void CheckStyle(UiContext context)
        {
            if (StyleDirty)
            {
                resolvedStyle = OnRestyle(context);
                StyleDirty = false;
            }
        }

        protected virtual ElementStyle OnRestyle(UiContext context)
        {
            return new StyleResolver()
                .Add(style)
                .Add(WidthProperty)
                .Add(HeightProperty)
                .Add(BackgroundProperty)
                .Add(BorderProperty)
                .Create<ElementStyle>();
        }


        public virtual void OnLayout(UiContext context, Layout layout, double delta)
        {
            CheckStyle(context);
            var screen = layout.Place(new(X, Y, resolvedStyle.Width, resolvedStyle.Height), Anchor);
            ClientRectangle = new(screen.X, screen.Y, screen.Width, screen.Height);
            UpdateAnimation(delta);
        }

        private static TextAlignment CastAlign(HorizontalAlignment h)
        {
            if (h == HorizontalAlignment.Center)
            {
                return TextAlignment.Center;
            }

            if (h == HorizontalAlignment.Right)
            {
                return TextAlignment.Right;
            }

            return TextAlignment.Left;
        }

        protected void RenderText(
            UiContext context,
            DrawList2D drawList,
            ref CachedRenderString? cache,
            RectangleF myRectangle,
            float textSize,
            string? font,
            InterfaceColor? textColor,
            InterfaceColor? shadowColor,
            HorizontalAlignment horizontalAlign,
            VerticalAlignment verticalAlign,
            bool clip,
            string text,
            float alpha = 1f,
            bool wrap = false
        )
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (myRectangle.Width <= 1 || myRectangle.Height <= 1)
            {
                return;
            }

            if (string.IsNullOrEmpty(font))
            {
                font = "$Normal";
            }

            if (textSize <= 0)
            {
                textSize = 10;
            }

            var color = (textColor ?? InterfaceColor.White).GetColor(context.GlobalTime);
            color.A *= alpha;

            if (color.A < float.Epsilon)
            {
                return;
            }

            var fnt = context.Data.GetFont(font);
            var size = context.TextSize(textSize);
            var lineHeight = context.RenderContext.Renderer2D.LineHeight(fnt, size);
            var drawRect = context.PointsToPixels(myRectangle);
            var sz = context.RenderContext.Renderer2D.MeasureStringCached(ref cache, fnt, size, text, false,
                shadowColor != null, CastAlign(horizontalAlign),
                wrap ? drawRect.Width : 0);

            // workaround for font substitution causing layout issues - e.g. CJK
            // TODO: How to get max lineheight of fonts in string?
            if (sz.Y > lineHeight && sz.Y < (lineHeight * 2))
            {
                lineHeight = sz.Y;
            }

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

            switch (verticalAlign)
            {
                case VerticalAlignment.Top:
                    drawY = drawRect.Y;
                    break;
                case VerticalAlignment.Bottom:
                    drawY = drawRect.Y + drawRect.Height - lineHeight;
                    break;
                default: // Center
                    drawY = drawRect.Y + (drawRect.Height / 2) - lineHeight / 2;
                    break;
            }

            var shadow = new OptionalColor();

            if (shadowColor != null)
            {
                shadow = new OptionalColor(shadowColor.Color);
                shadow.Color.A *= alpha;
            }

            if (clip && !drawList.PushClip(drawRect))
            {
                return;
            }

            drawList.DrawStringCached(ref cache, fnt, size, text, drawX, drawY, color, false,
                shadow, CastAlign(horizontalAlign),
                wrap ? drawRect.Width : 0);

            if (clip)
            {
                drawList.PopClip();
            }
        }

        public abstract void Render(UiContext context, double delta, DrawList2D drawList);

        public virtual void UnFocus()
        {
        }

        protected UiAnimation? CurrentAnimation;
        private float aspectRatio = 1;
        private Vector2? animSetPos;

        protected void UpdateAnimation(double delta)
        {
            if (CurrentAnimation == null)
            {
                if (animSetPos != null)
                {
                    ClientRectangle = ClientRectangle with { X = animSetPos.Value.X, Y = animSetPos.Value.Y };
                }
                return;
            }

            CurrentAnimation.SetWidgetRectangle(ClientRectangle);
            CurrentAnimation.Update(delta, aspectRatio);
            if (CurrentAnimation.Running)
            {
                ClientRectangle = ClientRectangle with { X = CurrentAnimation.CurrentPosition.X, Y  = CurrentAnimation.CurrentPosition.Y };
                return;
            }

            if (CurrentAnimation.FinalPositionSet.HasValue)
            {
                animSetPos = CurrentAnimation.FinalPositionSet.Value;
                ClientRectangle = ClientRectangle with { X = animSetPos.Value.X, Y  = animSetPos.Value.Y };
            }
            else
            {
                animSetPos = null;
            }
            CurrentAnimation = null;
        }

        public virtual void Update(UiContext context, double delta)
        {
            aspectRatio = context.ViewportWidth / context.ViewportHeight;
            Callback?.Invoke(delta);
        }

        private event Action<double>? Callback;

        public void OnUpdate(Closure handler)
        {
            Callback += (x) => { handler.Call(x); };
        }

        private event Action? EscapePressed;

        public void OnEscape(Closure handler)
        {
            EscapePressed += () => handler.Call();
        }

        public void Animate(string name, float offsetTime, float duration)
        {
            switch (name.ToLowerInvariant())
            {
                case "flyinleft":
                    var left = new FlyInLeft(offsetTime, duration)
                    {
                        From = -10
                    };
                    CurrentAnimation = left;
                    CurrentAnimation.Begin(aspectRatio);
                    break;
                case "flyinright":
                    var right = new FlyInRight(offsetTime, duration);
                    CurrentAnimation = right;
                    CurrentAnimation.Begin(aspectRatio);
                    break;
                case "flyoutleft":
                    var outleft = new FlyOutLeft(offsetTime, duration)
                    {
                        To = -10
                    };
                    CurrentAnimation = outleft;
                    CurrentAnimation.Begin(aspectRatio);
                    break;
                case "flyoutright":
                    var outright = new FlyOutRight(offsetTime, duration);
                    CurrentAnimation = outright;
                    CurrentAnimation.Begin(aspectRatio);
                    break;
                case "flyinbottom":
                    var inbottom = new FlyInBottom( offsetTime, duration);
                    CurrentAnimation = inbottom;
                    CurrentAnimation.Begin(aspectRatio);
                    break;
                case "flyoutbottom":
                    var outbottom = new FlyOutBottom(offsetTime, duration);
                    CurrentAnimation = outbottom;
                    CurrentAnimation.Begin(aspectRatio);
                    break;
            }
        }

        public override string ToString()
        {
            return $"{ID ?? "(no id)"} - {GetType()}";
        }

        public virtual bool MouseWanted(UiContext context, float x, float y)
        {
            return false;
        }

        [WattleScriptHidden]
        public virtual bool WantsEscape() => Visible && EscapePressed != null;

        [WattleScriptHidden]
        public virtual void OnEscapePressed()
        {
            if (Visible)
            {
                EscapePressed?.Invoke();
            }
        }

        public virtual void OnMouseDown(UiContext context)
        {
        }

        public virtual void OnMouseClick(UiContext context)
        {
        }

        public virtual void OnMouseDoubleClick(UiContext context)
        {
        }

        public virtual void OnMouseWheel(UiContext context, float delta)
        {
        }

        public virtual void OnMouseUp(UiContext context)
        {
        }

        public virtual void OnKeyDown(UiContext context, Keys key, bool control)
        {
        }

        public virtual void OnTextInput(string text)
        {
        }

        public virtual UiWidget? GetElement(string elementID)
        {
            if (string.IsNullOrWhiteSpace(elementID))
            {
                return null;
            }

            return elementID.Equals(ID, StringComparison.OrdinalIgnoreCase) ? this : null;
        }

        public virtual void Dispose()
        {
        }
    }
}
