// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer
{
	public class FlyInLeft : UiAnimation
	{
        public float From = -2;

		public FlyInLeft(double start, double time) : base(start, time)
		{
		}

        protected override void Run (double currentTime, float aspectRatio)
        {
            CurrentPosition.X = Easing.Ease(EasingTypes.EaseOut,
                (float)currentTime,
                0,
                (float)Duration,
                From - ClientRectangle.Width,
                ClientRectangle.X
            );
            CurrentPosition.Y = ClientRectangle.Y;
        }
	}
}

