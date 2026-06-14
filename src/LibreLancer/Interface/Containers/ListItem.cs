// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class ListItem : UiWidget
    {
        public Panel? ItemA {
            get;
            set;
        }
        public Panel? ItemB { get; set; }

        public float ItemMarginX { get; set; }
        public UiRenderable? SelectedBorder { get; set; }
        public UiRenderable? HoverBorder { get; set; }

        public bool Selected = false;
        public bool DoSelect = false;
        public bool Hovered = false;


        class FixedLayout : Layout
        {
            public bool AllowWidth;

            public FixedLayout(RectangleF parent, bool allowWidth) : base(parent)
            {
                AllowWidth = allowWidth;
            }

            public override RectangleF Place(RectangleF child, AnchorKind anchor) =>
                AllowWidth
                    ? Parent with { Width = child.Width }
                    : Parent;
        }

        public override void OnLayout(UiContext context, Layout layout, double delta)
        {
            base.OnLayout(context, layout, delta);
            float x = ClientRectangle.X;
            if (ItemA != null)
            {
                var l = new FixedLayout(ClientRectangle, ItemB != null);
                ItemA.OnLayout(context, l, delta);
                x += ItemA.ClientRectangle.Width;
            }
            x += ItemMarginX;
            if (ItemB != null)
            {
                var l2 = new FixedLayout(ClientRectangle with
                {
                    X = x,
                    Width = ClientRectangle.Width - (x - ClientRectangle.X)
                }, false);
                ItemB.OnLayout(context, l2, delta);
            }
        }

        public override void OnMouseClick(UiContext context)
        {
            DoSelect = false;
            if (!Visible) return;
            if (Enabled)
            {
                ItemA?.OnMouseClick(context);
                ItemB?.OnMouseClick(context);
                if(ItemA?.ClientRectangle.Contains(context.MouseX, context.MouseY) == true ||
                   ItemB?.ClientRectangle.Contains(context.MouseX, context.MouseY) == true)
                {
                    DoSelect = true;
                }
            }
        }

        public override void OnMouseDown(UiContext context)
        {
            if (!Visible) return;
            if (Enabled)
            {
                ItemA?.OnMouseUp(context);
                ItemB?.OnMouseDown(context);
            }
        }

        public override void OnMouseUp(UiContext context)
        {
            if (!Visible) return;
            if (Enabled)
            {
                ItemA?.OnMouseUp(context);
                ItemB?.OnMouseDown(context);
            }
        }

        public override void OnMouseWheel(UiContext context, float delta)
        {
            if (!Visible) return;
            if (Enabled)
            {
                ItemA?.OnMouseUp(context);
                ItemB?.OnMouseDown(context);
            }
        }

        public override void Update(UiContext context, double delta)
        {
            base.Update(context, delta);
            ItemA?.Update(context, delta);
            ItemB?.Update(context, delta);
            Hovered = (ItemA?.ClientRectangle.Contains(context.MouseX, context.MouseY) ?? false) ||
                      (ItemB?.ClientRectangle.Contains(context.MouseX, context.MouseY) ?? false);
        }

        public override void Render(UiContext context, double delta, DrawList2D drawList)
        {
            if (!Visible) return;
            Background?.Draw(context, drawList, ClientRectangle);
            ItemA?.Render(context, delta, drawList);
            ItemB?.Render(context, delta, drawList);
            UiRenderable? border = Border;
            if (Enabled)
            {
                if (Selected) border = SelectedBorder ?? border;
                if (Hovered) border = HoverBorder ?? border;
            }
            if(ItemA != null) border?.Draw(context, drawList, ItemA.ClientRectangle);
            if(ItemB != null) border?.Draw(context, drawList, ItemB.ClientRectangle);
        }
    }
}
