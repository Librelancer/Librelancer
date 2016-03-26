using System;
using OpenTK;
namespace LibreLancer
{
	public abstract class UIAnimation
	{
		public double Start;
		public double Duration;
		double time;
		public Vector2 CurrentPosition = Vector2.Zero;
		public Vector2? CurrentScale;
		public bool Running = false;

		protected UIAnimation (double start, double time)
		{
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
		}
	}
}

