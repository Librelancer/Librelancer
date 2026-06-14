// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
    public class FlyInRight : UiAnimation
    {
        public FlyInRight(double start, double time) : base(start, time)
        {
        }

        protected override void Run (double currentTime, float aspectRatio)
        {
            var from = 480 * aspectRatio;
            CurrentPosition.X = Easing.Ease(EasingTypes.EaseOut,
                (float)currentTime,
                0,
                (float)Duration,
                from,
                ClientRectangle.X
            );
            CurrentPosition.Y = ClientRectangle.Y;
        }
    }
}
