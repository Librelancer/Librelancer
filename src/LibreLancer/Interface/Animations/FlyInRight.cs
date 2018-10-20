// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	public class FlyInRight : UIAnimation
	{
		Vector2 finalPos;

		public FlyInRight(Vector2 final, double start, double time) : base(start, time)
		{
			finalPos = final;
			CurrentPosition.Y = finalPos.Y;
		}

		protected override void Run(double currentTime)
		{
			CurrentPosition.X = Utf.Ale.AlchemyEasing.Ease(
				Utf.Ale.EasingTypes.EaseOut,
				(float)currentTime,
				 0,
				(float)Duration,
				2,
				finalPos.X
			);
		}
	}
}

