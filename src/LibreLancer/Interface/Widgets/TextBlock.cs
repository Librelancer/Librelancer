// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer;

namespace LibreLancer.Interface
{
    [UiLoadable]
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
        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public InterfaceColor TextColor { get; set; }
        public InterfaceColor TextShadow { get; set; }
        public bool Clip { get; set; } = true;

        public bool Fill { get; set; } = false;
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X, myPos.Y, Width, Height);
            if (Fill) {
                myRectangle = parentRectangle;
            }
            myRectangle.X += MarginX;
            if (Background != null)
            {
                foreach(var elem in Background.Elements)
                    elem.Render(context, myRectangle);
            }
            var txt = txtAccess.GetText(context);
            if (!string.IsNullOrEmpty(txt))
                DrawText(context, myRectangle, TextSize, Font, TextColor, TextShadow, HorizontalAlignment, VerticalAlignment, Clip,
                    txt);
        }
    }
}