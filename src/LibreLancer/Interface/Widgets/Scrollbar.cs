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
    public class Scrollbar : UiWidget
    {
        private double lastTime = 0;
        private float timer = 1 / 8f;
        private int nextScrollDir = 0;
        private ScrollbarStyle style = new();

        public float ScrollOffset { get; set; }
        public float Tick { get; set; } = 0.1f;
        public bool Smooth { get; set; }
        public float ThumbSize { get; set; } = 0.75f;
        public float InsetY { get; set; }

        private bool updateThumb = true;

        protected override ElementStyle OnRestyle(UiContext context)
        {
            style = new StyleResolver()
                .Add(context.Data.Stylesheet?.Styles.DefaultStyle<ScrollbarStyle>())
                .Add(Style)
                .Add(WidthProperty)
                .Add(BackgroundProperty)
                .Add(BorderProperty)
                .Create<ScrollbarStyle>();
            upbutton.Style = style.UpButton;
            downbutton.Style = style.DownButton;
            thumb.Style = style.Thumb;
            thumbTop.Style = style.ThumbTop;
            thumbBottom.Style = style.ThumbBottom;
            return style;
        }

        private Button upbutton = new()
        {
            Anchor = AnchorKind.TopCenter
        };

        private Button downbutton = new()
        {
            Anchor = AnchorKind.BottomCenter
        };
        private Button thumb = new();
        private Button thumbTop = new();
        private Button thumbBottom = new();

        private RectangleF track;

        public override void OnLayout(UiContext context, Layout layout, double delta)
        {
            // We don't size like a regular control
            CheckStyle(context);
            var screen = layout.Place(new(0, InsetY, style.Width, Math.Max(layout.Parent.Height - (InsetY * 2), 1)), AnchorKind.TopRight);
            ClientRectangle = new(screen.X, screen.Y, screen.Width, screen.Height);
            UpdateAnimation(delta);
            // layout children
            var widthAdjust = style.ButtonMarginX * 2;
            upbutton.Width = ClientRectangle.Width - widthAdjust;
            downbutton.Width = ClientRectangle.Width - widthAdjust;

            var self = new Layout(ClientRectangle);
            upbutton.OnLayout(context, self, delta);
            downbutton.OnLayout(context, self, delta);

            track = ClientRectangle;
            track.Y += upbutton.ClientRectangle.Height;
            track.Height -= (upbutton.ClientRectangle.Height + downbutton.ClientRectangle.Height);
            track = track.Pad(style.TrackMarginX, style.TrackMarginY);
            thumb.Height = track.Height * ThumbSize;
            thumb.Y = ScrollOffset * (track.Height - thumb.Height);
            thumb.Width = track.Width;
            if (thumb.Dragging && (track.Height - thumb.Height) >= 0.01f)
            {
                var newY = MathHelper.Clamp(thumb.DragOffset.Y + dragYStart, 0, track.Height - thumb.Height);
                ScrollOffset = newY / (track.Height - thumb.Height);
            }
            thumb.OnLayout(context, self, delta);
        }

        private float dragYStart;
        public override void OnMouseDown(UiContext context)
        {
            if (!Visible) return;
            thumb.OnMouseDown(context);
            upbutton.OnMouseDown(context);
            downbutton.OnMouseDown(context);
            if (thumb.Dragging)
                dragYStart = thumb.Y;
        }

        public override void OnMouseUp(UiContext context)
        {
            upbutton.OnMouseUp(context);
            downbutton.OnMouseUp(context);
            thumb.OnMouseUp(context);
            dragYStart = 0;
        }

        public override void OnMouseWheel(UiContext context, float delta)
        {
            if (!Visible) return;
            if (Smooth)
            {
                ScrollOffset -= Tick * 4 * delta;
                if (ScrollOffset > 1) ScrollOffset = 1;
                if (ScrollOffset < 0) ScrollOffset = 0;
            }
            else
            {
                nextScrollDir = delta > 0 ? -1 : 1;
            }
        }

        public override void Update(UiContext context, double delta)
        {
            base.Update(context, delta);
            upbutton.Visible = downbutton.Visible = thumb.Visible = Visible;

            upbutton.Update(context, delta);
            downbutton.Update(context, delta);
            thumb.Update(context, delta);

            timer -= (float)delta;
            timer = MathHelper.Clamp(timer, 0, 100);
            var tickmult = (float)(Smooth ? delta * 8 : 1);

            if (upbutton.HeldDown && (timer <= 0 || Smooth))
            {
                ScrollOffset -= Tick * tickmult;
                if (ScrollOffset < 0) ScrollOffset = 0;
                timer = 1 / 8f;
            }
            if (downbutton.HeldDown && (timer <= 0 || Smooth))
            {
                ScrollOffset += Tick * tickmult;
                if (ScrollOffset > 1) ScrollOffset = 1;
                timer = 1 / 8f;
            }

            ScrollOffset += nextScrollDir * Tick;
            ScrollOffset = MathHelper.Clamp(ScrollOffset, 0, 1);
            nextScrollDir = 0;
        }

        public override void Render(UiContext context, double delta, DrawList2D drawList)
        {
            if (!Visible)
                return;
            // background
            style.Background?.Draw(context, drawList, ClientRectangle);
            // draw buttons
            upbutton.Render(context, delta, drawList);
            downbutton.Render(context, delta, drawList);
            // draw track
            style.TrackArea?.Draw(context, drawList, track);
            // draw thumb
            float top = 0, bottom = 0;
            if (style.ThumbTop != null)
            {
                top = style.ThumbTop.Height;
                var rect = new RectangleF(track.X, track.Y + thumb.Y, track.Width, top + 1);
                thumbTop.Draw(context, drawList, rect, thumb.Hovered, thumb.HeldDown, thumb.Selected, true);
            }
            if (style.ThumbBottom != null)
            {
                bottom = style.ThumbBottom.Height;
                var rect = new RectangleF(track.X, track.Y + thumb.Y + thumb.Height - bottom - 1, track.Width, bottom + 1);
                thumbBottom.Draw(context, drawList, rect, thumb.Hovered, thumb.HeldDown, thumb.Selected, true);
            }
            var thumbRect = new RectangleF(track.X, track.Y + thumb.Y + top, thumb.Width, thumb.Height - top - bottom);
            thumb.Draw(context, drawList, thumbRect, thumb.Hovered, thumb.HeldDown, thumb.Selected, true);
            style.Border?.Draw(context, drawList,ClientRectangle);
        }
    }
}
