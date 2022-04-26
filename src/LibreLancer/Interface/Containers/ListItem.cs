// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class ListItem : UiWidget
    {
        public Panel ItemA { get; set; }
        public Panel ItemB { get; set; }

        public float ItemMarginX { get; set; }
        public UiRenderable SelectedBorder { get; set; }
        public UiRenderable HoverBorder { get; set; }
        
        private RectangleF rectangleA;
        private RectangleF rectangleB;

        public bool Selected = false;
        public bool DoSelect = false;
        
        RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            Update(context, myPos);
            myPos = AnimatedPosition(myPos);
            var myRect = new RectangleF(myPos.X,myPos.Y, Width, Height);
            
            //layout children
            float x = 0;
            if (ItemA != null) {
                ItemA.Height = Height;
                ItemA.X = 0;
                ItemA.Y = 0;
                x = ItemA.Width;
                if (ItemB == null) ItemA.Width = Width;
                rectangleA = new RectangleF(myPos.X, myPos.Y, ItemA.Width, Height); 
            }
            
            x += ItemMarginX;
            if (ItemB != null)
            {
                ItemB.X = x;
                ItemB.Y = 0;
                ItemB.Width = Width - x;
                ItemB.Height = Height;
                rectangleB = new RectangleF(myPos.X + x, myPos.Y, ItemB.Width, ItemB.Height);
            }
            return myRect;
        }
        public override void OnMouseClick(UiContext context, RectangleF parentRectangle)
        {
            DoSelect = false;
            if (!Visible) return;
            if (Enabled)
            {
                var myRectangle = GetMyRectangle(context, parentRectangle);
                ItemA?.OnMouseClick(context, myRectangle);
                ItemB?.OnMouseClick(context, myRectangle);
                if (rectangleA.Contains(context.MouseX, context.MouseY) ||
                    rectangleB.Contains(context.MouseX, context.MouseY))
                {
                    DoSelect = true;
                }
            }
        }

        public override void OnMouseDown(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            if (Enabled)
            {
                var myRectangle = GetMyRectangle(context, parentRectangle);
                ItemA?.OnMouseUp(context, myRectangle);
                ItemB?.OnMouseDown(context, myRectangle);
            }
        }

        public override void OnMouseUp(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            if (Enabled)
            {
                var myRectangle = GetMyRectangle(context, parentRectangle);
                ItemA?.OnMouseUp(context, myRectangle);
                ItemB?.OnMouseDown(context, myRectangle);
            }
        }

        public override void OnMouseWheel(UiContext context, RectangleF parentRectangle, float delta)
        {
            if (!Visible) return;
            if (Enabled)
            {
                var myRectangle = GetMyRectangle(context, parentRectangle);
                ItemA?.OnMouseUp(context, myRectangle);
                ItemB?.OnMouseDown(context, myRectangle);
            }
        }

        public override void ApplyStylesheet(Stylesheet sheet)
        {
            base.ApplyStylesheet(sheet);
            ItemA?.ApplyStylesheet(sheet);
            ItemB?.ApplyStylesheet(sheet);
        }

        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            var myRectangle = GetMyRectangle(context, parentRectangle);
            Background?.Draw(context, myRectangle);
            ItemA?.Render(context, myRectangle);
            ItemB?.Render(context, myRectangle);
            UiRenderable border = Border;
            if (Enabled)
            {
                if (Selected) border = SelectedBorder ?? border;
                if (rectangleA.Contains(context.MouseX, context.MouseY) ||
                    rectangleB.Contains(context.MouseX, context.MouseY))
                    border = HoverBorder ?? border;
            }
            if(ItemA != null) border?.Draw(context, rectangleA);
            if(ItemB != null) border?.Draw(context, rectangleB);
        }
    }
}