// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class Scrollable : Container
    {
        public Scrollbar Scrollbar = new Scrollbar();

        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X,myPos.Y, Width, Height);
            foreach(var child in Children)
                child.Render(context, myRectangle);
            Scrollbar.Render(context, myRectangle);
        }

        public override void ApplyStylesheet(Stylesheet sheet)
        {
            Scrollbar.ApplyStyle(sheet);
            base.ApplyStylesheet(sheet);
        }

        public override void OnMouseDown(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X,myPos.Y, Width, Height);
            Scrollbar.OnMouseDown(context, myRectangle);
        }

        public override void OnMouseUp(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X,myPos.Y, Width, Height);
            Scrollbar.OnMouseUp(context, myRectangle);
        }
    }
}