// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
	public class FlyOutLeft : UiAnimation
	{
        public float To = -2;

		public FlyOutLeft( double start, double time) : base(start, time)
		{
            Remain = true;
		}

        public override void SetWidgetRectangle(RectangleF rect)
        {
            base.SetWidgetRectangle(rect);
            if (Time <= Start) CurrentPosition.X = ClientRectangle.X;
            CurrentPosition.Y = ClientRectangle.Y;
            FinalPositionSet = new(To - ClientRectangle.Width, ClientRectangle.Y);
        }


		protected override void Run(double currentTime, float aspectRatio)
		{
			CurrentPosition.X = Easing.Ease(
				EasingTypes.EaseOut,
				(float)currentTime,
				 0,
				(float)Duration,
				ClientRectangle.X,
				To - ClientRectangle.Width
			);
		}
	}
}

