// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
    public enum EasingTypes : byte
	{
        //Matches Alchemy types
        Step = 0,
		Linear = 1,
		EaseIn = 2,
		EaseOut = 3,
		EaseInOut = 4,
        EaseAuto = 5
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
            var x = (time - t1) / (t2 - t1);
            switch (type)
            {
                default:
                case EasingTypes.Step:
                    return v1;
                case EasingTypes.Linear:
                    return MathHelper.Lerp(v1, v2, x);
                case EasingTypes.EaseIn:
                    return MathHelper.Lerp(v1, v2, x * x);
                case EasingTypes.EaseOut:
                    return MathHelper.Lerp(v1, v2, 1.0f - (1.0f - x) * (1.0f - x));
                case EasingTypes.EaseInOut:
                    return MathHelper.Lerp(v1, v2, x * x * (3.0f - 2 * x));
                case EasingTypes.EaseAuto:
                    return MathHelper.Lerp(v1, v2, v1 > v2 ?
                        1.0f - (1.0f - x) * (1.0f - x)
                        : x * x);
            }
        }
    }
}

