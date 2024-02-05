// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer;
using LibreLancer.Graphics.Text;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class TextBlock : UiWidget
    {
        public float TextSize { get; set; }
        public float MarginX { get; set; }
        public string Font { get; set; }
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

        public bool AllCaps
        {
            get => txtAccess.AllCaps;
            set => txtAccess.AllCaps = value;
        }

        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public InterfaceColor TextColor { get; set; }
        public InterfaceColor TextShadow { get; set; }
        public bool Clip { get; set; } = true;

        public bool Wrap { get; set; } = false;

        public bool Fill { get; set; } = false;

        public float TextAlpha { get; set; } = 1;

        private CachedRenderString renderCache;

        private bool fading = true;
        private float fadeStep = 0;

        public void FadeIn(float duration)
        {
            if (!Visible) {
                Visible = true;
                TextAlpha = 0;
                fadeStep = 1.0f / duration;
            }
        }

        public void FadeOut(float duration)
        {
            if (Visible && fadeStep <= 0) {
                Visible = true;
                fadeStep = -(1.0f / duration);
                if (!fading) {
                    fading = true;
                }
            }
        }

        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (fading) {
                TextAlpha += (float) (context.DeltaTime * fadeStep);
                if (TextAlpha > 1) {
                    TextAlpha = 1;
                    fading = false;
                    fadeStep = 0;
                }
                if (TextAlpha < 0) {
                    TextAlpha = 1;
                    Visible = false;
                    fading = false;
                    fadeStep = 0;
                }
            }
            if (!Visible) return;
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X, myPos.Y, Width, Height);
            if (Fill) {
                myRectangle = parentRectangle;
            }
            if (Background != null)
            {
                foreach(var elem in Background.Elements)
                    elem.Render(context, myRectangle);
            }
            myRectangle.X += MarginX;
            myRectangle.Width -= MarginX * 2;
            var txt = txtAccess.GetText(context);
            if (!string.IsNullOrEmpty(txt))
                DrawText(context, ref renderCache, myRectangle, TextSize, Font, TextColor, TextShadow, HorizontalAlignment, VerticalAlignment, Clip,
                    txt, TextAlpha, Wrap);
        }
    }
}
