// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Utf.Ale
{
    public struct ColorKeyframe
    {
        public float Time;
        public Color3f Value;
        public ColorKeyframe(float time, Color3f value)
        {
            Time = time;
            Value = value;
        }
    }

	public class AlchemyColors
	{
		public float SParam;
		public EasingTypes Type;
        public RefList<ColorKeyframe> Keyframes = [new(0, Color3f.White)];
		public AlchemyColors ()
		{
		}
		public Color3f GetValue(float time)
		{
			if (Keyframes.Count == 1)
            {
				return Keyframes [0].Value;
			}

            if (time <= Keyframes[0].Time)
            {
                return Keyframes[0].Value;
            }

            if (time >= Keyframes[^1].Time)
            {
                return Keyframes[^1].Value;
            }

            int left = 0;
            int right = Keyframes.Count;

            while (left < right)
            {
                int mid = (left + right) >> 1;

                if (Keyframes[mid].Time <= time)
                    left = mid + 1;
                else
                    right = mid;
            }

            int i1 = left - 1;
            int i2 = left;

            float t1 = Keyframes[i1].Time;
            float t2 = Keyframes[i2].Time;
            Color3f v1 = Keyframes[i1].Value;
            Color3f v2 = Keyframes[i2].Value;
			// Interpolate!
			return Easing.EaseColorRGB(Type,time, t1, t2, v1, v2);
		}
	}
}

