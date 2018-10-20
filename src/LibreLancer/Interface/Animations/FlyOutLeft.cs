// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	public class FlyOutLeft : UIAnimation
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

		protected override void Run(double currentTime)
		{
			CurrentPosition.X = Utf.Ale.AlchemyEasing.Ease(
				Utf.Ale.EasingTypes.EaseOut,
				(float)currentTime,
				 0,
				(float)Duration,
				finalPos.X,
				To
			);
		}
	}
}

