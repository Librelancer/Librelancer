// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer;

namespace LibreLancer.Interface
{
    [UiLoadable]
    public class TextBlock : UiWidget
    {
        public float TextSize { get; set; }
        public string Font { get; set; }
        public string Text { get; set; }
        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public InterfaceColor TextColor { get; set; }
        public InterfaceColor TextShadow { get; set; }
        public bool Clip { get; set; } = true;
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X, myPos.Y, Width, Height);
            if (Background != null)
            {
                foreach(var elem in Background.Elements)
                    elem.Render(context, myRectangle);
            }

            if (!string.IsNullOrEmpty(Text))
                DrawText(context, myRectangle, TextSize, Font, TextColor, TextShadow, HorizontalAlignment, VerticalAlignment, Clip,
                    Text);
        }
    }
}