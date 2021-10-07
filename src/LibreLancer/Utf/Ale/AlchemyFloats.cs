// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Utf.Ale
{
	public class AlchemyFloats
	{
		public float SParam;
		public EasingTypes Type;
		public ValueTuple<float,float>[] Data;
		public AlchemyFloats ()
		{
		}
		public float GetValue(float time) {
			//Only have one keyframe? Just return it.
			if (Data.Length == 1) {
				return Data [0].Item2;
			}
			//Locate the keyframes to interpolate between
			float t1 = float.NegativeInfinity;
			float t2 = 0, v1 = 0, v2 = 0;
			for (int i = 0; i < Data.Length - 1; i++) {
				if (time >= Data [i].Item1 && time <= Data [i + 1].Item1) {
					t1 = Data [i].Item1;
					t2 = Data [i + 1].Item1;
					v1 = Data [i].Item2;
					v2 = Data [i + 1].Item2;
                    break;
                }
			}
			//Time wasn't between any values. Return max.
			if (t1 == float.NegativeInfinity) {
				return Data [Data.Length - 1].Item2;
			}
			//Interpolate!
			return Easing.Ease(Type,time, t1, t2, v1, v2);
		}

        public float GetMax(bool abs)
        {
            float max = 0;
            foreach (var i in Data)
            {
                var x = abs ? Math.Abs(i.Item2) : i.Item1;
                if (x > max) max = x;
            }
            return max;
        }
	}
}

