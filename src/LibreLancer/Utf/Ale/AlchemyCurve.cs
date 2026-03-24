// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Utf.Ale
{
	public class AlchemyCurve
	{
		public float SParam;
		public float Value;
		public LoopFlags Flags;
        public RefList<CurveKeyframe> Keyframes = [];

        public bool IsCurve;

        public bool Animates => IsCurve;

        public float GetMax(bool abs)
        {
            if (!IsCurve)
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
			if (!IsCurve)
				return Value;
			if (Keyframes.Count == 1)
				return Keyframes [0].Value;
			if (time <= Keyframes[0].Time)
				return Keyframes[0].Value;
			if (time >= Keyframes[^1].Time)
			{
				switch (Flags)
				{
					case LoopFlags.PlayOnce:
						return Keyframes[^1].Value;
                    case LoopFlags.Repeat:
                        time = time % Keyframes[^1].Time;
                        break;
                    default:
                        return Keyframes[^1].Value;
				}
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

            return ValueAt(Keyframes[left - 1], Keyframes[left], time);
		}

        private static float ValueAt(CurveKeyframe a, CurveKeyframe b, float t)
        {
            var dt = b.Time - a.Time;
            var dt2 = dt * 0.5f;

            var t0 = (t - a.Time) / dt;
            var ax = a.Value;
            var bx = b.Value;

            var a0 = MathF.FusedMultiplyAdd(a.Start, dt2, ax);
            var b0 = MathF.FusedMultiplyAdd(-b.End, dt2, bx);

            var a1 = MathHelper.Lerp( ax, a0, t0);
            var b1 = MathHelper.Lerp( a0, bx, t0);

            var a2 = MathHelper.Lerp( ax, b0, t0);
            var b2 = MathHelper.Lerp( b0, bx, t0);

            var a3 = MathHelper.Lerp( a1, a2, t0);
            var b3 = MathHelper.Lerp( b1, b2, t0);

            return MathHelper.Lerp( a3, b3, t0);
        }
	}
}

