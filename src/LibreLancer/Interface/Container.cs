// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer;

namespace LibreLancer.Interface
{ 
    public class Container : UiWidget
    {
        [UiContent]
        public List<UiWidget> Children { get; set; } = new List<UiWidget>();
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            ProcessAddChildren(context);
            if (!Visible) return;
            foreach(var child in Children)
                child.Render(context, parentRectangle);
        }

        protected void ProcessAddChildren(UiContext context)
        {
            while (addRemoves.TryDequeue(out var ac))
                ac(context);
        }
        
        Queue<Action<UiContext>> addRemoves = new Queue<Action<UiContext>>();
        public void AddChild(UiWidget child)
        {
            addRemoves.Enqueue((ctx) =>
            {
                child.ApplyStylesheet(ctx.Data.Stylesheet);
                Children.Add(child);
            });
        }
        public void RemoveChild(UiWidget child)
        {
           addRemoves.Enqueue((x) => { Children.Remove(child); });
        }
        public override void UnFocus()
        {
            foreach (var child in Children)
                child.UnFocus();
        }

        public override void ApplyStylesheet(Stylesheet sheet)
        {
            foreach(var child in Children)
                child.ApplyStylesheet(sheet);
        }

        public override bool MouseWanted(UiContext context, RectangleF parentRectangle, float x, float y)
        {
            if (!Visible)
                return false;
            foreach (var child in Children)
            {
                if(child.MouseWanted(context, parentRectangle, x, y))
                    return true;
            }
            return false;
        }

        public override void OnMouseClick(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            foreach(var child in Children)
                child.OnMouseClick(context, parentRectangle);
        }

        public override void OnMouseDoubleClick(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            foreach(var child in Children)
                child.OnMouseDoubleClick(context, parentRectangle);
        }

        public override void OnMouseDown(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            foreach(var child in Children)
                child.OnMouseDown(context, parentRectangle);
        }

        public override void OnMouseUp(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            foreach(var child in Children)
                child.OnMouseUp(context, parentRectangle);
        }

        public override void OnMouseWheel(UiContext context, RectangleF parentRectangle, float delta)
        {
            if (!Visible) return;
            foreach(var child in Children)
                child.OnMouseWheel(context, parentRectangle, delta);
        }

        public override UiWidget GetElement(string elementID)
        {
            if (string.IsNullOrWhiteSpace(elementID)) return null;
            if (elementID.Equals(ID, StringComparison.OrdinalIgnoreCase)) return this;
            foreach (var child in Children)
            {
                UiWidget w;
                if ((w = child.GetElement(elementID)) != null) return w;
            }
            return null;
        }

        public override bool WantsEscape()
        {
            if (!Visible) return false;
            if (base.WantsEscape()) return true;
            foreach(var child in Children)
                if (child.WantsEscape())
                    return true;
            return false;
        }

        public override void OnEscapePressed()
        {
            if (!Visible) return;
            base.OnEscapePressed();
            foreach(var child in Children)
                child.OnEscapePressed();
        }
    }
    
}