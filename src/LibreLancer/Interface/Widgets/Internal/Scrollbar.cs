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
            if (thumb.Dragging && (track.Height - thumb.Height) >= 0.01f) {
                var newY = MathHelper.Clamp(thumb.DragOffset.Y + dragYStart, 0, track.Height - thumb.Height);
                ScrollOffset = newY / (track.Height - thumb.Height);
            }
        }

        private TimeSpan lastTime = TimeSpan.Zero;
        private float timer = 1 / 8f;
        public void Render(UiContext context, RectangleF parent)
        {
            float delta = 0;
            if (lastTime == TimeSpan.Zero) {
                lastTime = context.GlobalTime;
            }
            else {
                var newTime = context.GlobalTime - lastTime;
                lastTime = context.GlobalTime;
                delta = (float) newTime.TotalSeconds;
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
            //draw track
            Style?.TrackArea?.Draw(context, track);
            //draw thumb
            thumb.Render(context, track);
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
        
    }
}