// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using LibreLancer;

namespace LibreLancer.Interface
{
    public class Scrollbar
    {
        public ScrollbarStyle Style;

        public float ScrollOffset { get; set; }
        public float Tick { get; set; } = 0.1f;

        public bool Smooth { get; set; }
        public float ThumbSize { get; set; } = 0.75f;

        private bool updateThumb = true;
        
        public void ApplyStyle(Stylesheet stylesheet)
        {
            Style = stylesheet.Lookup<ScrollbarStyle>(null);
            if (Style != null)
            {
                upbutton.SetStyle(Style.UpButton);
                downbutton.SetStyle(Style.DownButton);
                thumb.SetStyle(Style.Thumb);
                thumbTop.SetStyle(Style.ThumbTop);
                thumbBottom.SetStyle(Style.ThumbBottom);
            }
        }
        private Button upbutton = new Button()
        {
            Anchor = AnchorKind.TopCenter
        };
        
        private Button downbutton = new Button()
        {
            Anchor = AnchorKind.BottomCenter
        };
        private Button thumb = new Button();
        private Button thumbTop = new Button();
        private Button thumbBottom = new Button();
        void Layout(RectangleF parent, out RectangleF myRectangle, out RectangleF track)
        {
            myRectangle = new RectangleF(parent.X + parent.Width - Style.Width, parent.Y, Style.Width, parent.Height);
            var widthAdjust = (Style?.ButtonMarginX ?? 0) * 2;
            upbutton.Width = myRectangle.Width - widthAdjust;
            downbutton.Width = myRectangle.Width - widthAdjust;
            track = myRectangle;
            track.Y += upbutton.GetDimensions().Y;
            track.Height -= (upbutton.GetDimensions().Y + downbutton.GetDimensions().Y);
            if (Style != null)
            {
                track.X += Style.TrackMarginX;
                track.Width -= Style.TrackMarginX * 2;
                track.Y += Style.TrackMarginY;
                track.Height -= Style.TrackMarginY * 2;
            }
            thumb.Height = track.Height * ThumbSize;
            thumb.Y = ScrollOffset * (track.Height - thumb.Height);
            thumb.Width = track.Width;
            if (thumb.Dragging && (track.Height - thumb.Height) >= 0.01f)
            {
                var newY = MathHelper.Clamp(thumb.DragOffset.Y + dragYStart, 0, track.Height - thumb.Height);
                ScrollOffset = newY / (track.Height - thumb.Height);
            }
        }

        private double lastTime = 0;
        private float timer = 1 / 8f;
        private int nextScrollDir = 0;
        public void Render(UiContext context, RectangleF parent)
        {
            float delta = 0;
            if (lastTime == 0) {
                lastTime = context.GlobalTime;
            }
            else {
                var newTime = context.GlobalTime - lastTime;
                lastTime = context.GlobalTime;
                delta = (float) newTime;
                timer -= delta;
            }
            timer = MathHelper.Clamp(timer, 0, 100);
            Layout(parent, out var myRectangle, out var track);
            //background
            Style?.Background?.Draw(context, myRectangle);
            //draw buttons
            upbutton.Render(context, myRectangle);
            float tickmult = Smooth ? delta * 8 : 1;
            if (upbutton.HeldDown && (timer <= 0 || Smooth))
            {
                ScrollOffset -= Tick * tickmult;
                if (ScrollOffset < 0) ScrollOffset = 0;
                timer = 1 / 8f;
            }
            downbutton.Render(context, myRectangle);
            if (downbutton.HeldDown && (timer <= 0 || Smooth))
            {
                ScrollOffset += Tick * tickmult;
                if (ScrollOffset > 1) ScrollOffset = 1;
                timer = 1 / 8f;
            }
            //process non smooth scroll wheel
            ScrollOffset += nextScrollDir * Tick;
            ScrollOffset = MathHelper.Clamp(ScrollOffset, 0, 1);
            nextScrollDir = 0;
            //draw track
            Style?.TrackArea?.Draw(context, track);
            //draw thumb
            thumb.Update(context, track);
            float top = 0, bottom = 0;
            if (Style.ThumbTop != null)
            {
                top = Style.ThumbTop.Height;
                var rect = new RectangleF(track.X, track.Y + thumb.Y, track.Width, top + 1);
                thumbTop.Draw(context, rect, thumb.Hovered, thumb.HeldDown, thumb.Selected, true);
            }
            if (Style.ThumbBottom != null)
            {
                bottom = Style.ThumbBottom.Height;
                var rect = new RectangleF(track.X, track.Y + thumb.Y + thumb.Height - bottom - 1, track.Width, bottom + 1);
                thumbBottom.Draw(context, rect, thumb.Hovered, thumb.HeldDown, thumb.Selected, true);
            }
            var thumbRect = new RectangleF(track.X, track.Y + thumb.Y + top, thumb.Width, thumb.Height - top - bottom);
            thumb.Draw(context, thumbRect, thumb.Hovered, thumb.HeldDown, thumb.Selected, true);
        }

        private float dragYStart;
        public void OnMouseDown(UiContext context, RectangleF parent)
        {
            Layout(parent, out var myRectangle, out var track);
            thumb.OnMouseDown(context, track);
            upbutton.OnMouseDown(context, myRectangle);
            downbutton.OnMouseDown(context, myRectangle);
            if (thumb.Dragging)
                dragYStart = thumb.Y;
        }

        public void OnMouseUp(UiContext context, RectangleF parent)
        {
            Layout(parent, out var myRectangle, out var track);
            upbutton.OnMouseUp(context, myRectangle);
            downbutton.OnMouseUp(context, myRectangle);
            thumb.OnMouseUp(context, track);
            dragYStart = 0;
        }

        public void OnMouseWheel(float delta)
        {
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
        
    }
}