// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
    public enum EasingTypes : byte
	{
        //Matches Alchemy types
		Linear = 1,
		EaseIn = 2,
		EaseOut = 3,
		EaseInOut = 4,
		Step = 5,
    }
	public static class Easing
	{
        public static Color3f EaseColorRGB(EasingTypes type, float time, float t1, float t2, Color3f c1, Color3f c2)
		{

			float r = Ease(type, time, t1, t2, c1.R, c2.R);
			float g = Ease(type, time, t1, t2, c1.G, c2.G);
			float b = Ease(type, time, t1, t2, c1.B, c2.B);

			return new Color3f(r, g, b);
		}

		public static float Ease(EasingTypes type, float time, float t1, float t2, float v1, float v2)
		{
			switch (type) { 
            case 0: //TODO: What is zero?
			case EasingTypes.Linear:
				return Linear (time, t1, t2, v1, v2);
			case EasingTypes.EaseIn:
				return EaseIn (time, t1, t2, v1, v2);
			case EasingTypes.EaseOut:
				return EaseOut (time, t1, t2, v1, v2);
			case EasingTypes.EaseInOut:
				return EaseInOut (time, t1, t2, v1, v2);
			case EasingTypes.Step:
				return Step(time,t1,t2,v1,v2);
            }
			throw new InvalidOperationException ();
		}

		static float Linear(float time, float t1, float t2, float v1, float v2)
		{
			var time_pct = (time - t1) / (t2 - t1);
			return v1 + (v2 - v1) * time_pct;
		}

		static float EaseIn(float time, float t1, float t2, float v1, float v2)
		{
			var x = (time - t1) / (t2 - t1);
			// very close approximation to cubic-bezier(0.42, 0, 1.0, 1.0)
			var y = (float)Math.Pow(x, 1.685);
			return v1 + (v2 - v1) * y;
		}

		static float EaseOut(float time, float t1, float t2, float v1, float v2)
		{
			var x = (time - t1) / (t2 - t1);
			// very close approximation to cubic-bezier(0, 0, 0.58, 1.0)
			var y = 1f - (float)Math.Pow (1 - x, 1.685);
			return v1 + (v2 - v1) * y;
		}


		static float EaseInOut(float time, float t1, float t2, float v1, float v2)
		{
			var t = (time - t1) / (t2 - t1);
			var y = t < 0.5f ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
			return v1 + (v2 - v1) * y;
		}

		static float Step(float time, float t1, float t2, float v1, float v2)
		{
            return v1;
		}
    }
}

