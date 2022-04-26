// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class ListBox : UiWidget
    {
        public Scrollbar Scrollbar = new Scrollbar() { Smooth = false };

        public float ItemHeight { get; set; } = 30;
        public bool AlwaysShowScrollbar { get; set; } = false;
        
        [UiContent]
        public List<ListItem> Children { get; set; } = new List<ListItem>();

        int MaxDisplayChildren()
        {
            return (int) (Height / ItemHeight);
        }

        int ScrollCount()
        {
            int c = Children.Count - MaxDisplayChildren();
            return c <= 0 ? 0 : c;
        }
        
        private int childOffset = 0;
        private int selectedIndex = -1;

        public int SelectedIndex
        {
            get
            {
                return selectedIndex;
            }
            set {
                UnselectAll();
                selectedIndex = value;
                if(value > 0 && value < Children.Count)
                    Children[selectedIndex].Selected = true;
            }
        }
        
        event Action SelectedIndexChanged;

        public void OnSelectedIndexChanged(WattleScript.Interpreter.Closure handler)
        {
            SelectedIndexChanged += () =>
            {
                handler.Call();
            };
        }

        void UnselectAll()
        {
            foreach (var c in Children) c.Selected = false;
        }

        private int _lastScroll = 0;
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X,myPos.Y, Width, Height);
            Background?.Draw(context, myRectangle);
            //Update scrolling
            int scrollCount = ScrollCount();
            if (scrollCount <= 0) {
                childOffset = 0;
                Scrollbar.ScrollOffset = 0;
                Scrollbar.Tick = 0;
                Scrollbar.ThumbSize = 1;
                _lastScroll = 0;
            } else if (scrollCount != _lastScroll) {
                _lastScroll = scrollCount;
                Scrollbar.ThumbSize = 1.0f - (Math.Min(scrollCount, 9) * 0.1f);
                Scrollbar.Tick = 1.0f / scrollCount;
                Scrollbar.ScrollOffset = childOffset / (float) scrollCount;
            } else {
                childOffset = (int)(Scrollbar.ScrollOffset * scrollCount);
            }
            
            for(int i = childOffset; i < childOffset + MaxDisplayChildren() && i < Children.Count; i++)
            {
                var child = Children[i];
                child.Height = ItemHeight;
                child.Width = Math.Max(Width - ((scrollCount > 0 || AlwaysShowScrollbar) ? Scrollbar.Style.Width + 2 : 0), 3);
                child.X = 0;
                child.Y = ItemHeight * (i - childOffset);
                child.Render(context, myRectangle);
            }
            if(scrollCount > 0 || AlwaysShowScrollbar)
                Scrollbar.Render(context, myRectangle);
            Border?.Draw(context,myRectangle);
        }

        public override void OnMouseWheel(UiContext context, RectangleF parentRectangle, float delta)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X,myPos.Y, Width, Height);
            if(myRectangle.Contains(context.MouseX, context.MouseY))
                Scrollbar.OnMouseWheel(delta);
        }

        public override void ApplyStylesheet(Stylesheet sheet)
        {
            Scrollbar.ApplyStyle(sheet);
            base.ApplyStylesheet(sheet);
            foreach(var item in Children)
                item.ApplyStylesheet(sheet);
        }

        public override void OnMouseDown(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X,myPos.Y, Width, Height);
            if(_lastScroll > 0 || AlwaysShowScrollbar)
                Scrollbar.OnMouseDown(context, myRectangle);
            for (int i = childOffset; i < childOffset + MaxDisplayChildren() && i < Children.Count; i++)
            {
                Children[i].OnMouseDown(context, myRectangle);
            }
        }

        public override void OnMouseUp(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X,myPos.Y, Width, Height);
            if(_lastScroll > 0 || AlwaysShowScrollbar)
                Scrollbar.OnMouseUp(context, myRectangle);
            for (int i = childOffset; i < childOffset + MaxDisplayChildren() && i < Children.Count; i++)
            {
                Children[i].OnMouseUp(context, myRectangle);
            }
        }
        
        public override void OnMouseClick(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X,myPos.Y, Width, Height);
            if(_lastScroll > 0 || AlwaysShowScrollbar)
                Scrollbar.OnMouseUp(context, myRectangle);
            for (int i = childOffset; i < childOffset + MaxDisplayChildren() && i < Children.Count; i++)
            {
                Children[i].OnMouseClick(context, myRectangle);
                if (i >= Children.Count) break;
                if (Children[i].DoSelect) {
                    UnselectAll();
                    Children[i].Selected = true;
                    selectedIndex = i;
                    SelectedIndexChanged?.Invoke();
                }
            }
        }
    }
}