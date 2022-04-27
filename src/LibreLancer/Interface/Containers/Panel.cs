// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class Panel : Container
    {
        public bool CaptureMouse { get; set; } = true;
        RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            Update(context, myPos);
            myPos = AnimatedPosition(myPos);
            var myRect = new RectangleF(myPos.X,myPos.Y, Width, Height);
            return myRect;
        }
        public override void OnMouseClick(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            var myRectangle = GetMyRectangle(context, parentRectangle);
            foreach(var child in Children)
                child.OnMouseClick(context, myRectangle);
        }

        public override void OnMouseDown(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            var myRectangle = GetMyRectangle(context, parentRectangle);
            foreach(var child in Children)
                child.OnMouseDown(context, myRectangle);
        }

        public override void OnMouseUp(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            var myRectangle = GetMyRectangle(context, parentRectangle);
            foreach(var child in Children)
                child.OnMouseUp(context, myRectangle);
        }
        
        public override void OnMouseDoubleClick(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            var myRectangle = GetMyRectangle(context, parentRectangle);
            foreach(var child in Children)
                child.OnMouseDoubleClick(context, myRectangle);
        }

        public override void OnMouseWheel(UiContext context, RectangleF parentRectangle, float delta)
        {
            if (!Visible) return;
            var myRectangle = GetMyRectangle(context, parentRectangle);
            foreach(var child in Children)
                child.OnMouseWheel(context, myRectangle, delta);
        }

        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            ProcessAddChildren(context);
            if (!Visible) return;
            var myRectangle = GetMyRectangle(context, parentRectangle);
            Background?.Draw(context, myRectangle);
            foreach(var child in Children)
                child.Render(context, myRectangle);
            Border?.Draw(context, myRectangle);
        }

        public override bool MouseWanted(UiContext context, RectangleF parentRectangle, float x, float y)
        {
            if (!Visible || !CaptureMouse) return false;
            var myRectangle = GetMyRectangle(context, parentRectangle);
            if (Background != null && myRectangle.Contains(x, y)) return true;
            return base.MouseWanted(context, myRectangle, x, y);
        }
    }
}