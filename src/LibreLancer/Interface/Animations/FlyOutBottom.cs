// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
    public class FlyOutBottom : UiAnimation
    {
        public float To = 480;

        public FlyOutBottom(double start, double time) : base(start, time)
        {
            Remain = true;
        }

        public override void SetWidgetRectangle(RectangleF rect)
        {
            base.SetWidgetRectangle(rect);
            if (Time <= Start) CurrentPosition.Y = ClientRectangle.Y;
            CurrentPosition.X = ClientRectangle.X;
            FinalPositionSet = new Vector2(ClientRectangle.X, To);
        }
        protected override void Run(double currentTime, float aspectRatio)
        {
            CurrentPosition.X = ClientRectangle.X;
            CurrentPosition.Y = Easing.Ease(
                EasingTypes.EaseOut,
                (float)currentTime,
                0,
                (float)Duration,
                ClientRectangle.Y,
                To
            );
        }
    }
}
