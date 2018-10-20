// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	public abstract class UIAnimation
	{
		public double Start;
		public double Duration;
		double time;
		public Vector2 CurrentPosition = Vector2.Zero;
		public Vector2? CurrentScale;
		public Vector2? FinalPositionSet;
		public bool Running = false;
        public bool Remain = false;
		protected UIAnimation (double start, double duration)
		{
			Start = start;
			Duration = duration;
		}

		public void Update(double delta)
		{
			time += delta;
			if (time >= (Start + Duration)) {
				Running = false;
				return;
			}
			if (time >= Start)
				Run (time - Start);
		}

		protected abstract void Run (double currentTime);

		public virtual void Begin()
		{
			time = 0;
			Running = true;
			Run(0);
		}
	}
}

