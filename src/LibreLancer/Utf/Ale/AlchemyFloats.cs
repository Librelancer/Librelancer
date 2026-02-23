// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

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
		public float GetValue(float time) {
			//Only have one keyframe? Just return it.
			if (Keyframes.Count == 1) {
				return Keyframes [0].Value;
			}
			//Locate the keyframes to interpolate between
			float t1 = float.NegativeInfinity;
			float t2 = 0, v1 = 0, v2 = 0;
			for (int i = 0; i < Keyframes.Count - 1; i++) {
				if (time >= Keyframes [i].Time && time <= Keyframes [i + 1].Time) {
					t1 = Keyframes [i].Time;
					t2 = Keyframes [i + 1].Time;
					v1 = Keyframes [i].Value;
					v2 = Keyframes [i + 1].Value;
                    break;
                }
			}
			//Time wasn't between any values. Return max.
			if (t1 == float.NegativeInfinity) {
				return Keyframes [Keyframes.Count - 1].Value;
			}
			//Interpolate!
			return Easing.Ease(Type,time, t1, t2, v1, v2);
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

