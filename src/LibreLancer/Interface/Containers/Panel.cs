// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Interface
{
    [UiLoadable]
    public class Panel : Container
    {
        public override void OnMouseClick(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X, myPos.Y, Width, Height);
            foreach(var child in Children)
                child.OnMouseClick(context, myRectangle);
        }

        public override void OnMouseDown(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X, myPos.Y, Width, Height);
            foreach(var child in Children)
                child.OnMouseDown(context, myRectangle);
        }

        public override void OnMouseUp(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X, myPos.Y, Width, Height);
            foreach(var child in Children)
                child.OnMouseDown(context, myRectangle);
        }

        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X, myPos.Y, Width, Height);
            Background?.Draw(context, myRectangle);
            foreach(var child in Children)
                child.Render(context, myRectangle);
        }
    }
}