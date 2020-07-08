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
            if (!Visible) return;
            foreach(var child in Children)
                child.Render(context, parentRectangle);
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

        public override void OnMouseClick(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            foreach(var child in Children)
                child.OnMouseClick(context, parentRectangle);
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
    }
    
}