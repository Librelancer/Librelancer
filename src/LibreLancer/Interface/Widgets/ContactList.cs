using System;
using LibreLancer.Graphics.Text;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class ContactList : UiWidget
    {
        public Scrollbar Scrollbar = new Scrollbar() { Smooth = false };
        public string Font { get; set; } = "$ListText";
        public int TextSize { get; set; }

        public InterfaceColor FriendlyColor { get; set; }

        public InterfaceColor HostileColor { get; set; }

        public InterfaceColor NeutralColor { get; set; }
        public InterfaceColor HoverColor { get; set; }
        public InterfaceColor SelectedColor { get; set; }
        public InterfaceColor TextShadow { get; set; }
        public int DisplayRowCount { get; set; } = 5;

        private IContactListData data;

        public void SetData(IContactListData data)
        {
            this.data = data;
        }

        int ScrollCount()
        {
            if (data == null) return 0;
            int c = data.Count - DisplayRowCount;
            return c <= 0 ? 0 : c;
        }

        private int childOffset = 0;

        public override void ApplyStylesheet(Stylesheet sheet)
        {
            base.ApplyStylesheet(sheet);
            Scrollbar.ApplyStyle(sheet);
        }

        RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            Update(context, myPos);
            myPos = AnimatedPosition(myPos);
            var myRect = new RectangleF(myPos.X,myPos.Y, Width, Height);
            return myRect;
        }

        RectangleF GetCell(RectangleF parentRect, int row)
        {
            var lineHeight = parentRect.Height / DisplayRowCount;
            var y = parentRect.Y + row * lineHeight;
            var width = parentRect.Width;
            return new RectangleF(parentRect.X + 1, y + 1, width - 2, lineHeight - 2);
        }

        bool CanRender()
        {
            return Width > 0 && Height > 0 &&
                   Visible;
        }

        public override void OnMouseWheel(UiContext context, RectangleF parentRectangle, float delta)
        {
            if (!CanRender() || data == null) return;
            var rect = GetMyRectangle(context, parentRectangle);
            if(rect.Contains(context.MouseX, context.MouseY))
                Scrollbar.OnMouseWheel(delta);
        }

        public override void OnMouseClick(UiContext context, RectangleF parentRectangle)
        {
            if (!CanRender() || data == null) return;
            var rect = GetMyRectangle(context, parentRectangle);
            if (_lastScroll > 0) {
                rect.Width -= Scrollbar.Style.Width;
            }
            var rowCount = Math.Min(DisplayRowCount, (data.Count - childOffset));
            for (int row = 0; row < rowCount; row++)
            {
                var c = GetCell(rect, row);
                if (c.Contains(context.MouseX, context.MouseY) &&
                    row + childOffset < data.Count)
                {
                        data.SelectIndex(row + childOffset);
                }
            }
        }

        public override void OnMouseDown(UiContext context, RectangleF parentRectangle)
        {
            if (!CanRender() || data == null) return;
            var rect = GetMyRectangle(context, parentRectangle);
            if (_lastScroll > 0)
            {
                Scrollbar.OnMouseDown(context, rect);
            }
        }

        public override void OnMouseUp(UiContext context, RectangleF parentRectangle)
        {
            if (!CanRender() || data == null) return;
            var rect = GetMyRectangle(context, parentRectangle);
            if (_lastScroll > 0) {
                Scrollbar.OnMouseUp(context, rect);
            }
        }

        private int _lastScroll = 0;
        private CachedRenderString[] rowStrings;

        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (!CanRender()) return;
            var rect = GetMyRectangle(context, parentRectangle);
            Background?.Draw(context, rect);
            if (data != null) {
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
                if (scrollCount > 0) {
                    Scrollbar.Render(context, rect);
                    rect.Width -= Scrollbar.Style.Width;
                }
                var rowCount = Math.Min(DisplayRowCount, (data.Count - childOffset));
                if (rowStrings == null || rowStrings.Length < rowCount) rowStrings = new CachedRenderString[DisplayRowCount];
                for (int row = 0; row < rowCount; row++)
                {
                    //Get row state
                    bool hovered = false;
                    var selected = data.IsSelected(row + childOffset);
                    var str = data.Get(row + childOffset);
                    if (!selected)
                    {
                        var c = GetCell(rect, row);
                        if (c.Contains(context.MouseX, context.MouseY)) {
                            hovered = true;
                        }
                    }
                    InterfaceColor textColor = null;
                    switch (data.GetAttitude(row + childOffset)) {
                        case RepAttitude.Friendly:
                            textColor = FriendlyColor;
                            break;
                        case RepAttitude.Hostile:
                            textColor = HostileColor;
                            break;
                        case RepAttitude.Neutral:
                            textColor = NeutralColor;
                            break;
                    }
                    //Render row
                    var rowColor = Cascade(textColor ?? InterfaceColor.White, hovered ? HoverColor : null,
                        selected ? SelectedColor : null);
                    var rowRect = GetCell(rect, row);
                    DrawText(context, ref rowStrings[row], rowRect, TextSize, Font, rowColor, TextShadow, HorizontalAlignment.Left,
                        VerticalAlignment.Default, true, str);
                }
            }
            Border?.Draw(context, rect);
        }
    }
}
