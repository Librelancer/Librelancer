// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
    public class FlyInBottom : UiAnimation
    {
        private Vector2 finalPos;
        public float From = 480;
        private RectangleF clientRectangle;

        public FlyInBottom(double start, double time) : base(start, time)
        {
        }

        public override void SetWidgetRectangle(RectangleF rect)
        {
            base.SetWidgetRectangle(rect);

        }

        protected override void Run (double currentTime, float aspectRatio)
        {
            CurrentPosition.X = clientRectangle.X;
            CurrentPosition.Y = Easing.Ease(EasingTypes.EaseOut,
                (float) currentTime,
                0,
                (float) Duration,
                From,
                clientRectangle.Y
            );
        }
    }
}
