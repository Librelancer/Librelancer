// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LibreLancer.Utf.Ale
{
    public struct FloatKeyframe
    {
        public float Time;
        public float Value;
        public FloatKeyframe(float t, float v)
        {
            Time = t;
            Value = v;
        }
    }

	public class AlchemyFloats
	{
		public float SParam;
        public EasingTypes Type = EasingTypes.Linear;
        public RefList<FloatKeyframe> Keyframes = [new(0, 0)];
		public AlchemyFloats ()
		{
		}
        public float GetValue(float time)
        {
            if (Keyframes.Count == 1)
                return Keyframes[0].Value;

            if (time <= Keyframes[0].Time)
                return Keyframes[0].Value;

            if (time >= Keyframes[^1].Time)
                return Keyframes[^1].Value;

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
            float v1 = Keyframes[i1].Value;
            float v2 = Keyframes[i2].Value;

            return Easing.Ease(Type, time, t1, t2, v1, v2);
        }

        public float GetMax(bool abs)
        {
            float max = 0;
            foreach (var i in Keyframes)
            {
                var x = abs ? Math.Abs(i.Value) : i.Value;
                if (x > max) max = x;
            }
            return max;
        }
	}
}

