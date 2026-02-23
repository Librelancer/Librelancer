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
			//Only have one keyframe? Just return it.
			if (Keyframes.Count == 1) {
				return Keyframes [0].Value;
			}
			//Locate the keyframes to interpolate between
			float t1 = float.NegativeInfinity;
			float t2 = 0;
			Color3f v1 = new Color3f(), v2 = new Color3f();
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
			return Easing.EaseColorRGB(Type,time, t1, t2, v1, v2);
		}
	}
}

