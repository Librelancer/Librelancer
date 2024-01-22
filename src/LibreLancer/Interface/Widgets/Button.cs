// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Xml.Schema;
using LibreLancer;
using LibreLancer.Graphics.Text;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class Button : UiWidget
    {
        public bool Selected { get; set; }
        public string Style { get; set; }
        public float TextSize { get; set; }
        public string FontFamily { get; set; }

        public float MarginLeft { get; set; }

        public float MarginRight { get; set; }

        InfoTextAccessor txtAccess = new InfoTextAccessor();
        public string Text
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
        public string MouseEnterSound { get; set; }
        public string MouseDownSound { get; set; }
        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public InterfaceColor TextColor { get; set; }
        public InterfaceColor TextShadow { get; set; }

        public bool DebugTextFrame { get; set; }

        private ButtonStyle style;
        private bool styleSetManual = false;
        public void SetStyle(ButtonStyle style)
        {
            this.style = style;
            styleSetManual = true;
        }

        private bool lastFrameMouseInside = false;
        string GetText(UiContext context) => txtAccess.GetText(context);

        private CachedRenderString textCache;

        internal void Draw(UiContext context, RectangleF myRectangle, bool hover, bool pressed, bool selected, bool enabled)
        {
            ButtonAppearance activeStyle = null;
            if(selected) activeStyle = style.Selected;
            if (hover) activeStyle = style?.Hover;
            if (pressed) activeStyle = style?.Pressed ?? style?.Hover;
            if (!enabled) activeStyle = style?.Disabled;
            var bk = Cascade(style?.Normal?.Background, activeStyle?.Background, Background);
            bk?.Draw(context, myRectangle);
            var border = Cascade(style?.Normal?.Border, activeStyle?.Border, Border);
            border?.Draw(context, myRectangle);
        }

        internal void Update(UiContext context, RectangleF parentRectangle)
        {
            var myRectangle = GetMyRectangle(context, parentRectangle);
            if (myRectangle.Contains(context.MouseX, context.MouseY))
            {
                Hovered = true;
                if (!lastFrameMouseInside)
                {
                    var sound = MouseEnterSound ?? style?.MouseEnterSound;
                    if (!string.IsNullOrWhiteSpace(sound)) {
                        context.PlaySound(sound);
                    }
                }
                lastFrameMouseInside = true;
            }
            else
            {
                Hovered = false;
                lastFrameMouseInside = false;
            }
            if (Dragging) {
                DragOffset = new Vector2(context.MouseX, context.MouseY) - DragStart;
            }
            if (HeldDown) {
                if (!myRectangle.Contains(context.MouseX, context.MouseY)) {
                    HeldDown = false;
                }
            }
        }

        public bool Hovered { get; set; }
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            Update(context, parentRectangle);
            ButtonAppearance activeStyle = null;
            var myRectangle = GetMyRectangle(context, parentRectangle);
            if (myRectangle.Contains(context.MouseX, context.MouseY)) {
                activeStyle = style?.Hover;
            }
            else {
            }
            if (HeldDown) {
                activeStyle = style?.Pressed ?? style?.Hover;
            }
            if (Selected) activeStyle = style?.Selected;
            if (!Enabled) activeStyle = style?.Disabled;
            var bk = Cascade(style?.Normal?.Background, activeStyle?.Background, Background);
            bk?.Draw(context, myRectangle);

            float mLeft = Cascade(style?.Normal?.MarginLeft, activeStyle?.MarginLeft, MarginLeft);
            float mRight = Cascade(style?.Normal?.MarginRight, activeStyle?.MarginRight, MarginRight);

            var txt = GetText(context);
            if (!string.IsNullOrEmpty(txt) && !string.IsNullOrWhiteSpace(txt))
            {
                var textRect = myRectangle;
                textRect.X += mLeft;
                textRect.Width -= mRight;
                if (DebugTextFrame)
                {
                    context.RenderContext.Renderer2D.DrawRectangle(context.PointsToPixels(textRect), Color4.Aqua, 1);
                }
                DrawText(
                    context,
                    ref textCache,
                    textRect,
                    Cascade(style?.Normal?.TextSize, activeStyle?.TextSize, TextSize),
                    Cascade(style?.Normal?.FontFamily, activeStyle?.FontFamily, FontFamily),
                    Cascade(style?.Normal?.TextColor, activeStyle?.TextColor, TextColor),
                    Cascade(style?.Normal?.TextShadow, activeStyle?.TextShadow, TextShadow),
                    Cascade(style?.Normal?.HorizontalAlignment, activeStyle?.HorizontalAlignment, HorizontalAlignment),
                    Cascade(style?.Normal?.VerticalAlignment, activeStyle?.VerticalAlignment, VerticalAlignment),
                    true,
                    txt
                );
            }
            var border = Cascade(style?.Normal?.Border, activeStyle?.Border, Border);
            border?.Draw(context, myRectangle);
        }

        RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
        {
            var width = Cascade(style?.Width, null, Width);
            var height = Cascade(style?.Height, null, Height);
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, width, height);
            Update(context, myPos);
            myPos = AnimatedPosition(myPos);
            var myRect = new RectangleF(myPos.X,myPos.Y, width, height);
            return myRect;
        }

        public bool HeldDown;
        public bool Dragging;
        public Vector2 DragStart;
        public Vector2 DragOffset;
        public override void OnMouseDown(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            if (CurrentAnimation != null) return;
            var myRect = GetMyRectangle(context, parentRectangle);
            if (myRect.Contains(context.MouseX, context.MouseY))
            {
                var sound = MouseDownSound ?? style?.MouseDownSound;
                if (!string.IsNullOrWhiteSpace(sound)) {
                    context.PlaySound(sound);
                }
                HeldDown = true;
                Dragging = true;
                DragStart = new Vector2(context.MouseX, context.MouseY);
            }
        }

        public override void OnMouseUp(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            Dragging = false;
            HeldDown = false;
            DragStart = DragOffset = Vector2.Zero;
        }


        event Action Clicked;

        public void OnClick(WattleScript.Interpreter.Closure handler)
        {
            Clicked += () =>
            {
                handler.Call();
            };
        }
        public override void OnMouseClick(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible || !Enabled) return;
            if (CurrentAnimation != null) return;
            var myRect = GetMyRectangle(context, parentRectangle);
            if (myRect.Contains(context.MouseX, context.MouseY)) {
                Clicked?.Invoke();
            }
        }

        public override Vector2 GetDimensions()
        {
            var width = Cascade(style?.Width, null, Width);
            var height = Cascade(style?.Height, null, Height);
            return new Vector2(width, height);
        }

        public override void ApplyStylesheet(Stylesheet sheet)
        {
            base.ApplyStylesheet(sheet);
            if(!styleSetManual) style = sheet.Lookup<ButtonStyle>(Style);
        }
    }
}
