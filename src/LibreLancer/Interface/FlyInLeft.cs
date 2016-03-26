using System;
using OpenTK;
namespace LibreLancer
{
	public class FlyInLeft : UIAnimation
	{
		Vector2 finalPos;
		public FlyInLeft(Vector2 final, double start, double time) : base(start, time)
		{
			finalPos = final;
			CurrentPosition.Y = finalPos.Y;
		}

		protected override void Run (double currentTime)
		{
			CurrentPosition.X = (float)Easings.Circular.EaseOut (
				currentTime, 
				-2,
				Math.Abs (finalPos.X - (-2)),
				Duration
			);
		}
	}
}

