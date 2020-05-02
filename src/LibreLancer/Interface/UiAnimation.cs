// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
	public abstract class UiAnimation
	{
		public double Start;
		public double Duration;
		protected double Time;
		public Vector2 CurrentPosition = Vector2.Zero;
		public Vector2? CurrentScale;
		public Vector2? FinalPositionSet;
		public bool Running = false;
        public bool Remain = false;
		protected UiAnimation (double start, double duration)
		{
			Start = start;
			Duration = duration;
		}

		public void Update(double delta, float aspectRatio)
		{
			Time += delta;
			if (Time >= (Start + Duration)) {
				Running = false;
				return;
			}

            if (Time >= Start)
                Run(Time - Start, aspectRatio);
        }

        public virtual void SetWidgetPosition(Vector2 pos)
        {
        }

        protected abstract void Run(double currentTime, float aspectRatio);

		public virtual void Begin(float aspectRatio)
		{
			Time = 0;
			Running = true;
            Run(0, aspectRatio);
        }
	}
}

