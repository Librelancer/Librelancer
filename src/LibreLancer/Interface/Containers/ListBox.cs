// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Graphics;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class ListBox : UiWidget
    {
        public float ItemHeight { get; set; } = 30;
        public float ItemWidth { get; set; }
        public bool OverlayScrollbar { get; set; } = false;
        public bool AlwaysShowScrollbar { get; set; } = false;

        public Scrollbar Scrollbar { get; set; } = new();

        [UiContent]
        public List<ListItem> Children { get; set; } = [];

        private int MaxDisplayChildren()
        {
            return (int) (Height / ItemHeight);
        }

        private int ScrollCount()
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

        private event Action? SelectedIndexChanged;

        public void OnSelectedIndexChanged(WattleScript.Interpreter.Closure handler)
        {
            SelectedIndexChanged += () =>
            {
                handler.Call();
            };
        }

        private void UnselectAll()
        {
            foreach (var c in Children) c.Selected = false;
        }
        private int currScrollCount;
        private int lastScroll = 0;

        public override void Update(UiContext context, double delta)
        {
            base.Update(context, delta);
            Scrollbar.Visible = AlwaysShowScrollbar || currScrollCount > 0;
            Scrollbar.Update(context, delta);
            int scrollCount = ScrollCount();
            if (scrollCount <= 0) {
                childOffset = 0;
                Scrollbar.ScrollOffset = 0;
                Scrollbar.Tick = 0;
                Scrollbar.ThumbSize = 1;
                lastScroll = 0;
            } else if (scrollCount != lastScroll) {
                lastScroll = scrollCount;
                Scrollbar.ThumbSize = 1.0f - (Math.Min(scrollCount, 9) * 0.1f);
                Scrollbar.Tick = 1.0f / scrollCount;
                Scrollbar.ScrollOffset = childOffset / (float) scrollCount;
            } else {
                childOffset = (int)(Scrollbar.ScrollOffset * scrollCount);
            }
            currScrollCount = scrollCount;
            // Update visible children
            for(int i = childOffset; i < childOffset + MaxDisplayChildren() && i < Children.Count; i++)
            {
                var child = Children[i];
                child.Update(context, delta);
            }
        }

        public override void OnLayout(UiContext context, Layout layout, double delta)
        {
            base.OnLayout(context, layout, delta);

            var self = new Layout(ClientRectangle);
            Scrollbar.OnLayout(context, self, delta);
            for(int i = childOffset; i < childOffset + MaxDisplayChildren() && i < Children.Count; i++)
            {
                var child = Children[i];
                var scrollVisible = currScrollCount > 0 || AlwaysShowScrollbar;
                var scrollbarWidth = Scrollbar.ClientRectangle.Width;
                child.Height = ItemHeight;
                child.Width = Math.Max(ItemWidth > 0 ? ItemWidth :
                    Width - ((scrollVisible && !OverlayScrollbar) ? scrollbarWidth + 2 : 0), 3);
                child.X = 0;
                child.Y = ItemHeight * (i - childOffset);
                child.OnLayout(context, self, delta);
            }
        }

        public override void Render(UiContext context, double delta, DrawList2D drawList)
        {
            Background?.Draw(context, drawList, ClientRectangle);
            // Update scrolling
            for(int i = childOffset; i < childOffset + MaxDisplayChildren() && i < Children.Count; i++)
            {
                var child = Children[i];
                child.Render(context, delta, drawList);
            }

            if (currScrollCount > 0 || AlwaysShowScrollbar)
                Scrollbar.Render(context, delta, drawList);
            Border?.Draw(context, drawList, ClientRectangle);
        }

        public override void OnMouseWheel(UiContext context, float delta)
        {
            if(ClientRectangle.Contains(context.MouseX, context.MouseY))
                Scrollbar.OnMouseWheel(context, delta);
        }

        public override void OnMouseDown(UiContext context)
        {
            Scrollbar.OnMouseDown(context);
            for (int i = childOffset; i < childOffset + MaxDisplayChildren() && i < Children.Count; i++)
            {
                Children[i].OnMouseDown(context);
            }
        }

        public override void OnMouseUp(UiContext context)
        {
            Scrollbar.OnMouseUp(context);
            for (int i = childOffset; i < childOffset + MaxDisplayChildren() && i < Children.Count; i++)
            {
                Children[i].OnMouseUp(context);
            }
        }

        public override void OnMouseClick(UiContext context)
        {
            Scrollbar.OnMouseUp(context);
            for (int i = childOffset; i < childOffset + MaxDisplayChildren() && i < Children.Count; i++)
            {
                Children[i].OnMouseClick(context);
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
