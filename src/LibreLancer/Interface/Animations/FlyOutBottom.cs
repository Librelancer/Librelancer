// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
    public class FlyOutBottom : UiAnimation
    {
        Vector2 finalPos;
        public float To = 480;
        public FlyOutBottom(Vector2 final, double start, double time) : base(start, time)
        {
            finalPos = final;
            CurrentPosition.X = finalPos.X;
            FinalPositionSet = new Vector2(finalPos.X, To);
            Remain = true;
        }

        public override void SetWidgetPosition(Vector2 pos)
        {
            finalPos = pos;
            if (Time <= Start) CurrentPosition.Y = finalPos.Y;
            CurrentPosition.X = finalPos.X;
            FinalPositionSet = new Vector2(finalPos.X, To);
        }
        protected override void Run(double currentTime, float aspectRatio)
        {
            CurrentPosition.Y = Easing.Ease(
                EasingTypes.EaseOut,
                (float)currentTime,
                0,
                (float)Duration,
                finalPos.Y,
                To
            );
        }
    }
}