// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer;
using LibreLancer.Graphics;
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
        public string Font { get; set; } = "";
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

        public bool AllCaps
        {
            get => txtAccess.AllCaps;
            set => txtAccess.AllCaps = value;
        }

        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public InterfaceColor? TextColor { get; set; }
        public InterfaceColor? TextShadow { get; set; }
        public bool Clip { get; set; } = true;

        public bool Wrap { get; set; } = false;

        public bool Fill { get; set; } = false;

        public float TextAlpha { get; set; } = 1;

        private CachedRenderString? renderCache;

        private bool fading = true;
        private float fadeStep = 0;

        public void FadeIn(float duration)
        {
            if (!Visible)
            {
                Visible = true;
                TextAlpha = 0;
                fadeStep = 1.0f / duration;
            }
        }

        public void FadeOut(float duration)
        {
            if (Visible && fadeStep <= 0)
            {
                Visible = true;
                fadeStep = -(1.0f / duration);

                if (!fading)
                {
                    fading = true;
                }
            }
        }

        public override void OnLayout(UiContext context, Layout layout, double delta)
        {
            base.OnLayout(context, layout, delta);
            if (Fill)
                ClientRectangle = layout.Fill();
        }

        public override void Update(UiContext context, double delta)
        {
            base.Update(context, delta);
            if (fading)
            {
                TextAlpha += (float) (delta * fadeStep);
                if (TextAlpha > 1)
                {
                    TextAlpha = 1;
                    fading = false;
                    fadeStep = 0;
                }

                if (TextAlpha < 0)
                {
                    TextAlpha = 1;
                    Visible = false;
                    fading = false;
                    fadeStep = 0;
                }
            }
        }

        public override void Render(UiContext context, double delta, DrawList2D drawList)
        {
            if (!Visible) return;
            var rect = ClientRectangle;

            Background?.Draw(context, drawList, ClientRectangle);
            if (Background != null)
            {
                foreach (var elem in Background.Elements)
                    elem.Render(context, drawList, rect, 1);
            }

            rect.X += MarginX;
            rect.Width -= MarginX * 2;
            var txt = txtAccess.GetText(context);
            if (!string.IsNullOrEmpty(txt))
                RenderText(context, drawList, ref renderCache, rect, TextSize, Font, TextColor, TextShadow,
                    HorizontalAlignment, VerticalAlignment, Clip,
                    txt, TextAlpha, Wrap);
            Border?.Draw(context, drawList, ClientRectangle);
        }
    }
}
