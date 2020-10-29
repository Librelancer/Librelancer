// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
	public class FlyOutLeft : UiAnimation
	{
		Vector2 finalPos;
        public float To = -2;
		public FlyOutLeft(Vector2 final, double start, double time) : base(start, time)
		{
			finalPos = final;
			CurrentPosition.Y = finalPos.Y;
			FinalPositionSet = new Vector2(To, finalPos.Y);
            Remain = true;
		}

        public override void SetWidgetPosition(Vector2 pos)
        {
            finalPos = pos;
            if (Time <= Start) CurrentPosition.X = finalPos.X;
            CurrentPosition.Y = finalPos.Y;
            FinalPositionSet = new Vector2(To, finalPos.Y);
        }
		protected override void Run(double currentTime, float aspectRatio)
		{
			CurrentPosition.X = Easing.Ease(
				EasingTypes.EaseOut,
				(float)currentTime,
				 0,
				(float)Duration,
				finalPos.X,
				To
			);
		}
	}
}

