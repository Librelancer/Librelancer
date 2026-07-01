// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
    public class FlyOutRight : UiAnimation
    {
        public float To;

        public FlyOutRight(double start, double time) : base(start, time)
        {
            Remain = true;
        }

        public override void SetWidgetRectangle(RectangleF rect)
        {
            base.SetWidgetRectangle(rect);
            CurrentPosition.Y = rect.Y;
            FinalPositionSet = new(To, rect.Y);
        }

        protected override void Run(double currentTime, float aspectRatio)
        {
            To = 480 * aspectRatio + ClientRectangle.Width;
            FinalPositionSet = new Vector2(To, ClientRectangle.Y);
            CurrentPosition.X = Easing.Ease(
                EasingTypes.EaseOut,
                (float)currentTime,
                0,
                (float)Duration,
                ClientRectangle.X,
                To
            );
        }
    }
}
