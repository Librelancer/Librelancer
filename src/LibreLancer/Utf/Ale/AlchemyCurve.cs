// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

namespace LibreLancer.Utf.Ale
{
	public class AlchemyCurve
	{
		public float SParam;
		public float Value;
		public LoopFlags Flags;
		public List<CurveKeyframe> Keyframes;

        public bool Animates
        {
            get { return Keyframes != null && Keyframes.Count != 1; }
        }

        public float GetMax(bool abs)
        {
            if (Keyframes == null)
                return abs ? Math.Abs(Value) : Value;
            float max = 0;
            foreach (var k in Keyframes)
            {
                var x = abs ? Math.Abs(k.Value) : k.Value;
                if (x > max) max = x;
            }
            return max;
        }
        public float GetValue(float time) {
			if (Keyframes == null)
				return Value;
			if (Keyframes.Count == 1)
				return Keyframes [0].Value;
			if (time <= Keyframes[0].Time)
				return Keyframes[0].Value;
			if (time >= Keyframes[Keyframes.Count - 1].Time)
			{
				switch (Flags)
				{
					case LoopFlags.PlayOnce:
						return Keyframes[Keyframes.Count - 1].Value;
                    case LoopFlags.Repeat:
                        time = time % Keyframes[Keyframes.Count - 1].Time;
                        break;
                    default:
                        return Keyframes[Keyframes.Count - 1].Value;
				}
				
			}
			for (int i = 0; i < Keyframes.Count - 1; i++)
			{
				var a = Keyframes[i];
				var b = Keyframes[i + 1];
                //TODO: Actually do this properly with InTangent and OutTangent
                if (time >= a.Time && time <= b.Time)
                {
                    if(Math.Abs(a.Time - b.Time) < float.Epsilon) return b.Value;
                    return ValueAt(a, b, time);
                }
            }
            return Keyframes[Keyframes.Count - 1].Value;
		}

        static float Linear(float t, float a, float b) =>
            a * (1 - t) + b * t;

        static float ValueAt(CurveKeyframe a, CurveKeyframe b, float t)
        {
            var dt = b.Time - a.Time;
            var t0 = (t - a.Time) / dt;
            var ax = a.Value;
            var bx = b.Value;

            var a0 = ax + a.Start * (dt * .5f);
            var b0 = bx - b.End * (dt * .5f);
            
            var a1 = Linear(t0, ax, a0);
            var b1 = Linear(t0, a0, bx);

            var a2 = Linear(t0, ax, b0);
            var b2 = Linear(t0, b0, bx);

            var a3 = Linear(t0, a1, a2);
            var b3 = Linear(t0, b1, b2);

            return Linear(t0, a3, b3);
        }
	}
}

