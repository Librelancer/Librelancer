// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using LibreLancer;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [WattleScriptUserData]
    [UiLoadable]
    public class HSlider : UiWidget
    {
        [WattleScriptHidden]
        public HSliderStyle Style;

        private float ScrollOffset;
        private float Tick = 0.1f;

        private bool _smooth = true;

        public bool Smooth
        {
            get { return _smooth; }
            set { _smooth = value;
                UpdateMinMax();
            }
        }

        private float ThumbSize = 0.75f;

        private float _min = 0;
        private float _max = 1;

        public float Min
        {
            get { return _min; }
            set { _min = value; UpdateMinMax(); }
        }

        public float Max
        {
            get { return _max; }
            set { _max = value; UpdateMinMax(); }
        }

        public float Value
        {
            get
            {
                var val = Min + (ScrollOffset) * (Max - Min);
                if (!_smooth) val = (int) val;
                return MathHelper.Clamp(val, Min, Max);
            }
            set
            {
                ScrollOffset = (value - Min) / (Max - Min);
            }
        }

        void UpdateMinMax()
        {
            if (_smooth)
            {
                ThumbSize = 0.5f;
            }
            else
            {
                var scrollCount = Max - Min;
                ThumbSize = 1.0f - (Math.Min(scrollCount, 9) * 0.1f);
                Tick = 1.0f / scrollCount;
            }
        }

        private bool updateThumb = true;
        
        public override void ApplyStylesheet(Stylesheet stylesheet)
        {
            Style = stylesheet.Lookup<HSliderStyle>(null);
            if (Style != null)
            {
                leftbutton.SetStyle(Style.LeftButton);
                rightbutton.SetStyle(Style.RightButton);
                thumb.SetStyle(Style.Thumb);
                thumbleft.SetStyle(Style.ThumbLeft);
                thumbright.SetStyle(Style.ThumbRight);
            }
        }
        private Button leftbutton = new Button()
        {
            Anchor = AnchorKind.CenterLeft
        };
        
        private Button rightbutton = new Button()
        {
            Anchor = AnchorKind.CenterRight
        };
        private Button thumb = new Button();
        private Button thumbleft = new Button();
        private Button thumbright = new Button();
        void Layout(UiContext context, RectangleF parent, out RectangleF myRectangle, out RectangleF track)
        {
            var height = Cascade(Style?.Height, null, Height);
            var myPos = context.AnchorPosition(parent, Anchor, X, Y, Width, height);
            myRectangle = new RectangleF(myPos.X, myPos.Y, Width, height);
            var heightAdjust = (Style?.ButtonMarginY ?? 0) * 2;
            leftbutton.Height = myRectangle.Height - heightAdjust;
            rightbutton.Height = myRectangle.Height - heightAdjust;
            track = myRectangle;
            track.X += leftbutton.GetDimensions().X;
            track.Width -= (leftbutton.GetDimensions().X + rightbutton.GetDimensions().X);
            if (Style != null)
            {
                track.X += Style.TrackMarginX;
                track.Width -= Style.TrackMarginX * 2;
                track.Y += Style.TrackMarginY;
                track.Height -= Style.TrackMarginY * 2;
            }
            thumb.Width = track.Width * ThumbSize;
            thumb.X = ScrollOffset * (track.Width - thumb.Width);
            thumb.Height = track.Height;
            if (thumb.Dragging && (track.Width - thumb.Width) >= 0.01f)
            {
                var newX = MathHelper.Clamp(thumb.DragOffset.X + dragXStart, 0, track.Width - thumb.Width);
                ScrollOffset = newX / (track.Width - thumb.Width);
            }
        }

        private double lastTime = 0;
        private float timer = 1 / 8f;
        private int nextScrollDir = 0;
        public override void Render(UiContext context, RectangleF parent)
        {
            if(!Visible) return;
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
            Layout(context, parent, out var myRectangle, out var track);
            //background
            Style?.Background?.Draw(context, myRectangle);
            //draw buttons
            leftbutton.Render(context, myRectangle);
            float tickmult = Smooth ? delta * 8 : 1;
            if (leftbutton.HeldDown && (timer <= 0 || Smooth))
            {
                ScrollOffset -= Tick * tickmult;
                if (ScrollOffset < 0) ScrollOffset = 0;
                timer = 1 / 8f;
            }
            rightbutton.Render(context, myRectangle);
            if (rightbutton.HeldDown && (timer <= 0 || Smooth))
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
            float left = 0, right = 0;
            if (Style.ThumbLeft != null)
            {
                left = Style.ThumbLeft.Width;
                var rect = new RectangleF(track.X + thumb.X, track.Y + thumb.Y, left + 1, track.Height);
                thumbleft.Draw(context, rect, thumb.Hovered, thumb.HeldDown, thumb.Selected, true);
            }
            if (Style.ThumbRight != null)
            {
                //bottom = Style.ThumbBottom.Height;
                right = Style.ThumbRight.Width;
                var rect = new RectangleF(track.X + thumb.X + thumb.Width - right - 1, thumb.Y + track.Y, right + 1, track.Height);
                thumbright.Draw(context, rect, thumb.Hovered, thumb.HeldDown, thumb.Selected, true);
            }
            var thumbRect = new RectangleF(track.X + thumb.X + left, track.Y, thumb.Width - left - right, thumb.Height);
            thumb.Draw(context, thumbRect, thumb.Hovered, thumb.HeldDown, thumb.Selected, true);
        }

        private float dragXStart;
        public override void OnMouseDown(UiContext context, RectangleF parent)
        {
            if (!Visible) return;
            Layout(context, parent, out var myRectangle, out var track);
            thumb.OnMouseDown(context, track);
            leftbutton.OnMouseDown(context, myRectangle);
            rightbutton.OnMouseDown(context, myRectangle);
            if (thumb.Dragging)
                dragXStart = thumb.X;
        }

        public override void OnMouseUp(UiContext context, RectangleF parent)
        {
            if (!Visible) return;
            Layout(context, parent, out var myRectangle, out var track);
            leftbutton.OnMouseUp(context, myRectangle);
            rightbutton.OnMouseUp(context, myRectangle);
            thumb.OnMouseUp(context, track);
            dragXStart = 0;
        }

        public override void OnMouseWheel(UiContext context, RectangleF parentRectangle, float delta)
        {
            if (!Visible) return;
            Layout(context, parentRectangle, out _, out var track);
            if (track.Contains(context.MouseX, context.MouseY))
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
}