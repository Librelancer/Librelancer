// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [WattleScriptUserData]
    [UiLoadable]
    public class HSlider : UiWidget
    {
        public HSlider()
        {
            UpdateMinMax();
        }

        private float ScrollOffset;
        private float Tick = 0.1f;

        private bool _smooth = true;

        public bool Smooth
        {
            get { return _smooth; }
            set
            {
                _smooth = value;
                UpdateMinMax();
            }
        }

        private float ThumbSize;
        private float _min = 0;
        private float _max = 1;

        public float Min
        {
            get { return _min; }
            set
            {
                _min = value;
                UpdateMinMax();
            }
        }

        public float Max
        {
            get { return _max; }
            set
            {
                _max = value;
                UpdateMinMax();
            }
        }

        public float Value
        {
            get
            {
                var val = Min + (ScrollOffset) * (Max - Min);
                if (!_smooth) val = (int) val;
                return MathHelper.Clamp(val, Min, Max);
            }
            set { ScrollOffset = (value - Min) / (Max - Min); }
        }

        private void UpdateMinMax()
        {
            if (_smooth)
            {
                ThumbSize = 0.5f;
            }
            else
            {
                var scrollCount = Max - Min;
                ThumbSize = Math.Clamp(1.0f - (Math.Min(scrollCount, 9) * 0.1f), 0.0f, 0.5f);
                Tick = 1.0f / scrollCount;
            }
        }

        private bool updateThumb = true;
        private float dragXStart;

        private HSliderStyle sliderStyle = new();

        protected override ElementStyle OnRestyle(UiContext context)
        {
            sliderStyle = new StyleResolver()
                .Add(context.Data.Stylesheet?.Styles.DefaultStyle<HSliderStyle>())
                .Add(Style)
                .Add(WidthProperty)
                .Add(HeightProperty)
                .Add(BackgroundProperty)
                .Add(BorderProperty)
                .Create<HSliderStyle>();

            leftbutton.Style = sliderStyle.LeftButton;
            rightbutton.Style = sliderStyle.RightButton;
            thumb.Style = sliderStyle.Thumb;
            thumbleft.Style = sliderStyle.ThumbLeft;
            thumbright.Style = sliderStyle.ThumbRight;

            return sliderStyle;
        }

        private Button leftbutton = new()
        {
            Anchor = AnchorKind.CenterLeft
        };

        private Button rightbutton = new()
        {
            Anchor = AnchorKind.CenterRight
        };

        private Button thumb = new();
        private Button thumbleft = new();
        private Button thumbright = new();
        private RectangleF track;

        public override void OnLayout(UiContext context, Layout layout, double delta)
        {
            base.OnLayout(context, layout, delta);
            var area = new Layout(ClientRectangle);
            var heightAdjust = sliderStyle.ButtonMarginY * 2;
            leftbutton.Height = ClientRectangle.Height - heightAdjust;
            rightbutton.Height = ClientRectangle.Height - heightAdjust;
            leftbutton.OnLayout(context, area, delta);
            rightbutton.OnLayout(context, area, delta);
            track = ClientRectangle;
            track.X += leftbutton.ClientRectangle.Width;
            track.Width -= leftbutton.ClientRectangle.Width + rightbutton.ClientRectangle.Width;

            track = track.Pad(sliderStyle.TrackMarginX, sliderStyle.TrackMarginY);
            thumb.Width = track.Width * ThumbSize;
            thumb.X = ScrollOffset * (track.Width - thumb.Width);
            thumb.Height = track.Height;

            if (thumb.Dragging && (track.Width - thumb.Width) >= 0.01f)
            {
                var newX = MathHelper.Clamp(thumb.DragOffset.X + dragXStart, 0, track.Width - thumb.Width);
                ScrollOffset = newX / (track.Width - thumb.Width);
            }

            thumb.OnLayout(context, new Layout(track), delta);
            /*thumbleft.OnLayout(area);
            thumbright.OnLayout(area);
            thumb.OnLayout(area);*/

        }

        public override void Update(UiContext context, double delta)
        {
            base.Update(context, delta);
            if (!Visible) return;
            leftbutton.Update(context,delta);
            rightbutton.Update(context, delta);
            thumb.Update(context, delta);
        }

        private double timer = 1 / 8f;
        private int nextScrollDir = 0;

        public override void Render(UiContext context, double delta, DrawList2D drawList)
        {
            if (!Visible) return;

            timer -= delta;
            timer = MathHelper.Clamp(timer, 0, 100);

            // background
            Background?.Draw(context, drawList, ClientRectangle);

            //draw buttons
            leftbutton.Render(context,delta, drawList);
            float tickmult = (float)(Smooth ? delta * 8 : 1);

            if (leftbutton.HeldDown && (timer <= 0 || Smooth))
            {
                ScrollOffset -= Tick * tickmult;
                if (ScrollOffset < 0) ScrollOffset = 0;
                timer = 1 / 8f;
            }

            rightbutton.Render(context, delta, drawList);

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
            sliderStyle.TrackArea?.Draw(context, drawList, track);
            //draw thumb
            float left = 0, right = 0;

            if (sliderStyle.ThumbLeft != null)
            {
                left = sliderStyle.ThumbLeft.Width;
                var rect = new RectangleF(track.X + thumb.X, track.Y + thumb.Y, left + 1, track.Height);
                thumbleft.Draw(context, drawList, rect, thumb.Hovered, thumb.HeldDown, thumb.Selected, true);
            }

            if (sliderStyle.ThumbRight != null)
            {
                //bottom = Style.ThumbBottom.Height;
                right = sliderStyle.ThumbRight.Width;
                var rect = new RectangleF(track.X + thumb.X + thumb.Width - right - 1, thumb.Y + track.Y, right + 1,
                    track.Height);
                thumbright.Draw(context, drawList, rect, thumb.Hovered, thumb.HeldDown, thumb.Selected, true);
            }

            var thumbRect = new RectangleF(track.X + thumb.X + left, track.Y, thumb.Width - left - right, thumb.Height);
            thumb.Draw(context, drawList, thumbRect, thumb.Hovered, thumb.HeldDown, thumb.Selected, true);
        }


        public override void OnMouseDown(UiContext context)
        {
            if (!Visible) return;
            thumb.OnMouseDown(context);
            leftbutton.OnMouseDown(context);
            rightbutton.OnMouseDown(context);
            if (thumb.Dragging)
                dragXStart = thumb.X;
        }

        public override void OnMouseUp(UiContext context)
        {
            if (!Visible) return;
            leftbutton.OnMouseUp(context);
            rightbutton.OnMouseUp(context);
            thumb.OnMouseUp(context);
            dragXStart = 0;
        }

        public override void OnMouseWheel(UiContext context, float delta)
        {
            if (!Visible) return;
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
