// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer;

namespace LibreLancer.Interface
{
    [UiLoadable]
    public class Gauge : UiWidget
    {
        public UiRenderable Fill { get; set; }
        public float PercentFilled { get; set; }

        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X, myPos.Y, Width, Height);
            Background?.Draw(context, myRectangle);
            var fillRect = myRectangle;
            fillRect.Width *= PercentFilled;
            Fill?.DrawWithClip(context, myRectangle, fillRect);
            Border?.Draw(context,myRectangle);
        }

    }
}