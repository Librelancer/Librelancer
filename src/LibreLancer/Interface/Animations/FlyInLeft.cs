// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
	public class FlyInLeft : UiAnimation
	{
		Vector2 finalPos;
        public float From = -2;
		public FlyInLeft(Vector2 final, double start, double time) : base(start, time)
		{
			finalPos = final;
			CurrentPosition.Y = finalPos.Y;
		}

        public override void SetWidgetPosition(Vector2 pos)
        {
            finalPos = pos;
            CurrentPosition.Y = finalPos.Y;
        }

        protected override void Run (double currentTime, float aspectRatio)
        {
            CurrentPosition.X = Easing.Ease(EasingTypes.EaseOut,
                (float)currentTime,
                0,
                (float)Duration,
                From,
                finalPos.X
            );
        }
	}
}

