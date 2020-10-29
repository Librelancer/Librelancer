// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
    public class FlyInBottom : UiAnimation
    {
        Vector2 finalPos;
        public float From = 480;
        public FlyInBottom(Vector2 final, double start, double time) : base(start, time)
        {
            finalPos = final;
            CurrentPosition.X = finalPos.X;
        }

        public override void SetWidgetPosition(Vector2 pos)
        {
            finalPos = pos;
            CurrentPosition.X = finalPos.X;
        }

        protected override void Run (double currentTime, float aspectRatio)
        {
            CurrentPosition.Y = Easing.Ease(EasingTypes.EaseOut,
                (float) currentTime,
                0,
                (float) Duration,
                From,
                finalPos.Y
            );
        }
    }
}