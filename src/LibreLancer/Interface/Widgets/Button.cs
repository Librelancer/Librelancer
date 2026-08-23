// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Xml.Schema;
using LibreLancer;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Text;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class Button : UiWidget
    {
        protected StyledProperty<string> fontFamily = new("FontFamily");
        protected StyledProperty<Metric> marginLeft = new("MarginLeft");
        protected StyledProperty<Metric> marginRight = new("MarginRight");
        protected StyledProperty<float> textSize = new("TextSize");
        protected StyledProperty<string?> mouseEnterSound = new("MouseEnterSound");
        protected StyledProperty<string> mouseDownSound = new("MouseDownSound");
        protected StyledProperty<HorizontalAlignment> horizontalAlignment = new("HorizontalAlignment");
        protected StyledProperty<VerticalAlignment> verticalAlignment = new("VerticalAlignment");
        protected StyledProperty<InterfaceColor?> textColor = new("TextColor");
        protected StyledProperty<InterfaceColor?> textShadow = new("TextShadow");

        public bool Selected { get; set; }

        public float TextSize
        {
            get => textSize.Value;
            set
            {
                textSize.Set(value);
                StyleDirty = true;
            }
        }

        public string? FontFamily
        {
            get => fontFamily.Value;
            set
            {
                fontFamily.Set(value);
                StyleDirty = true;
            }
        }

        public Metric MarginLeft
        {
            get => marginLeft.Value;
            set
            {
                marginLeft.Set(value);
                StyleDirty = true;
            }
        }

        public Metric MarginRight
        {
            get => marginRight.Value;
            set
            {
                marginRight.Set(value);
                StyleDirty = true;
            }
        }


        public string? MouseEnterSound
        {
            get => mouseEnterSound.Value;
            set
            {
                mouseEnterSound.Set(value);
                StyleDirty = true;
            }
        }

        public string? MouseDownSound
        {
            get => mouseDownSound.Value;
            set
            {
                mouseDownSound.Set(value);
                StyleDirty = true;
            }
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get => horizontalAlignment.Value;
            set
            {
                horizontalAlignment.Set(value);
                StyleDirty = true;
            }
        }

        public VerticalAlignment VerticalAlignment
        {
            get => verticalAlignment.Value;
            set
            {
                verticalAlignment.Set(value);
                StyleDirty = true;
            }
        }

        public InterfaceColor? TextColor
        {
            get => textColor.Value;
            set
            {
                textColor.Set(value);
                StyleDirty = true;
            }
        }

        public InterfaceColor? TextShadow
        {
            get => textShadow.Value;
            set
            {
                textShadow.Set(value);
                StyleDirty = true;
            }
        }

        public bool DrawText { get; set; } = true;

        private InfoTextAccessor txtAccess = new();

        public string? Text
        {
            get => txtAccess.Text;
            set => txtAccess.Text = value;
        }

        public int Strid
        {
            get => txtAccess.Strid;
            set => txtAccess.Strid = value;
        }

        public int InfoId
        {
            get => txtAccess.InfoId;
            set => txtAccess.InfoId = value;
        }

        public bool DebugTextFrame { get; set; }

        private bool lastFrameMouseInside = false;
        private string GetText(UiContext context) => txtAccess.GetText(context);

        private CachedRenderString? textCache;
        protected ButtonStyle ButtonStyle = new();
        protected StyledButton Appearance = new();

        protected enum ButtonState
        {
            Normal,
            Selected,
            Hover,
            Pressed,
            Disabled
        }

        protected ButtonState State
        {
            get;
            set
            {
                if (value != field)
                    OnStyleChanged();
                field = value;
            }
        }

        protected class StyledButton : ElementStyle
        {
            private StyledProperty<float> textSize = new("TextSize");
            private StyledProperty<Metric> marginLeft = new("MarginLeft");
            private StyledProperty<Metric> marginRight = new("MarginRight");
            private StyledProperty<string> fontFamily = new("FontFamily");
            private StyledProperty<HorizontalAlignment> horizontalAlignment = new("HorizontalAlignment");
            private StyledProperty<VerticalAlignment> verticalAlignment = new("VerticalAlignment");
            private StyledProperty<InterfaceColor> textColor = new("TextColor");
            private StyledProperty<InterfaceColor?> textShadow = new("TextShadow");

            public float TextSize => textSize.Value;
            public Metric MarginLeft => marginLeft.Value;
            public Metric MarginRight => marginRight.Value;
            public string FontFamily => fontFamily.Value ?? "$Normal";
            public HorizontalAlignment HorizontalAlignment => horizontalAlignment.Value;
            public VerticalAlignment VerticalAlignment => verticalAlignment.Value;
            public InterfaceColor TextColor => textColor.Value ?? Color4.White;
            public InterfaceColor? TextShadow => textShadow.Value;

            public override void Set(StyleResolver resolver) =>
                resolver.Add(WidthProperty)
                    .Add(HeightProperty)
                    .Add(BorderProperty)
                    .Add(BackgroundProperty)
                    .Add(marginLeft)
                    .Add(marginRight)
                    .Add(textSize)
                    .Add(fontFamily)
                    .Add(horizontalAlignment)
                    .Add(verticalAlignment)
                    .Add(textColor)
                    .Add(textShadow);

            public override void Create(StyleResolver resolver) =>
                resolver.Query(WidthProperty)
                    .Query(HeightProperty)
                    .Query(BorderProperty)
                    .Query(BackgroundProperty)
                    .Query(marginLeft)
                    .Query(marginRight)
                    .Query(textSize)
                    .Query(fontFamily)
                    .Query(horizontalAlignment)
                    .Query(verticalAlignment)
                    .Query(textColor)
                    .Query(textShadow);
        }

        protected override ElementStyle OnRestyle(UiContext context)
        {
            ButtonStyle = new StyleResolver()
                .Add(context.Data.Stylesheet?.Styles.DefaultStyle<ButtonStyle>())
                .Add(Style)
                .Add(WidthProperty)
                .Add(HeightProperty)
                .Add(mouseEnterSound)
                .Add(mouseDownSound)
                .Create<ButtonStyle>();
            var stateApp = State switch
            {
                ButtonState.Selected => ButtonStyle.Selected,
                ButtonState.Hover => ButtonStyle.Hover,
                ButtonState.Pressed => ButtonStyle.Pressed,
                ButtonState.Disabled => ButtonStyle.Disabled,
                _ => null
            };

            var res = new StyleResolver()
                .Add(ButtonStyle)
                .Add(ButtonStyle.Normal)
                .Add(stateApp)
                .Add(marginLeft)
                .Add(marginRight)
                .Add(textSize)
                .Add(fontFamily)
                .Add(horizontalAlignment)
                .Add(verticalAlignment)
                .Add(textColor)
                .Add(textShadow)
                .Add(BackgroundProperty)
                .Add(BorderProperty);
            Appearance = res.Create<StyledButton>();
            return Appearance;
        }

        internal void Draw(UiContext context, DrawList2D drawList, RectangleF myRectangle, bool hover, bool pressed,
            bool selected,
            bool enabled)
        {
            var s = ButtonState.Normal;
            if (!enabled)
                s = ButtonState.Disabled;
            else if (pressed)
                s = ButtonState.Pressed;
            else if (hover)
                s = ButtonState.Hover;
            else if (selected)
                s = ButtonState.Selected;
            State = s;
            CheckStyle(context);
            Appearance.Background?.Draw(context, drawList, myRectangle);
            Appearance.Border?.Draw(context, drawList, myRectangle);
        }

        public override void Update(UiContext context, double delta)
        {
            base.Update(context, delta);
            if (ClientRectangle.Contains(context.MouseX, context.MouseY))
            {
                Hovered = true;

                if (!lastFrameMouseInside)
                {
                    if (!string.IsNullOrWhiteSpace(ButtonStyle.MouseEnterSound))
                    {
                        context.PlaySound(ButtonStyle.MouseEnterSound);
                    }
                }

                lastFrameMouseInside = true;
            }
            else
            {
                Hovered = false;
                lastFrameMouseInside = false;
            }

            if (Dragging)
            {
                DragOffset = new Vector2(context.MouseX, context.MouseY) - DragStart;
            }

            if (HeldDown)
            {
                if (!ClientRectangle.Contains(context.MouseX, context.MouseY))
                {
                    HeldDown = false;
                }
            }

            if (!Enabled)
                State = ButtonState.Disabled;
            else if (HeldDown)
                State = ButtonState.Pressed;
            else if (Hovered)
                State = ButtonState.Hover;
            else if (Selected)
                State = ButtonState.Selected;
            else
                State = ButtonState.Normal;
        }


        public bool Hovered { get; set; }

        public override void Render(UiContext context, double delta, DrawList2D drawList)
        {
            if (!Visible) return;
            CheckStyle(context);
            string txt = GetText(context);

            if (State == ButtonState.Hover && !DrawText &&
                !string.IsNullOrWhiteSpace(txt))
            {
                context.SetTooltip(txt, ClientRectangle);
            }

            if (State == ButtonState.Hover && Strid != 0)
            {
                context.SetRollover(Strid);
            }

            Appearance.Background?.Draw(context, drawList, ClientRectangle);

            if (DrawText && !string.IsNullOrWhiteSpace(txt))
            {
                var textRect = ClientRectangle;
                var ml = Appearance.MarginLeft.ToPoint(ClientRectangle, MetricAxis.X);
                var mr = Appearance.MarginRight.ToPoint(ClientRectangle, MetricAxis.X);
                textRect.X += ml;
                textRect.Width -= ml + mr;

                if (DebugTextFrame)
                {
                    drawList.DrawRectangle(context.PointsToPixels(textRect), Color4.Aqua, 1);
                }

                RenderText(
                    context,
                    drawList,
                    ref textCache,
                    textRect,
                    Appearance.TextSize,
                    Appearance.FontFamily,
                    Appearance.TextColor,
                    Appearance.TextShadow,
                    Appearance.HorizontalAlignment,
                    Appearance.VerticalAlignment,
                    true,
                    txt
                );
            }

            Appearance.Border?.Draw(context, drawList, ClientRectangle);
        }


        public bool HeldDown;
        public bool Dragging;
        public Vector2 DragStart;
        public Vector2 DragOffset;

        public override void OnMouseDown(UiContext context)
        {
            if (!Visible) return;
            if (CurrentAnimation != null) return;

            if (ClientRectangle.Contains(context.MouseX, context.MouseY))
            {
                // While we don't have better cascade
                var sound = ButtonStyle.MouseDownSound;

                if (!string.IsNullOrWhiteSpace(sound))
                {
                    context.PlaySound(sound);
                }

                HeldDown = true;
                Dragging = true;
                DragStart = new Vector2(context.MouseX, context.MouseY);
            }
        }

        public override void OnMouseUp(UiContext context)
        {
            Dragging = false;
            HeldDown = false;
            DragStart = DragOffset = Vector2.Zero;
        }

        private event Action<UiContext>? Clicked;

        public void OnClick(WattleScript.Interpreter.Closure handler)
        {
            Clicked += _ => { handler.Call(); };
        }

        [WattleScriptHidden]
        public void OnClick(Action<UiContext> action)
        {
            Clicked += action;
        }

        public void ClearClick()
        {
            Clicked = null;
        }

        public override bool MouseWanted(UiContext context, float x, float y) =>
            ClientRectangle.Contains(x, y);

        public override void OnMouseClick(UiContext context)
        {
            if (!Visible || !Enabled) return;
            if (CurrentAnimation != null) return;

            if (ClientRectangle.Contains(context.MouseX, context.MouseY))
            {
                Clicked?.Invoke(context);
            }
        }
    }
}
