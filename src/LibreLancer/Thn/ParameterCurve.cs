// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Thorn;

namespace LibreLancer.Thn
{
	public enum PCurveType
	{
		Unknown,
		FreeForm,
		BumpIn,
		BumpOut,
		RampDown,
		RampUp,
		Step,
		Smooth,
		ThornL, //?
		Linear,
		CatmullRom
	}
	public class ParameterCurve
	{
		public string CLSID;
		public PCurveType Type = PCurveType.Unknown;
		public List<Vector4> Points;
		public float Period;
        public ParameterCurve() { }

        public ParameterCurve(PCurveType type, IEnumerable<Vector4> points)
        {
            Type = type;
            Points = points.ToList();
            Period = -1;
        }
		public ParameterCurve(ThornTable table)
		{
			CLSID = (string)table["CLSID"];
			switch (CLSID.ToLowerInvariant())
			{
				case "freeformpcurve":
					Type = PCurveType.FreeForm;
					break;
				case "bumpinpcurve":
					Type = PCurveType.BumpIn;
					break;
				case "bumpoutpcurve":
					Type = PCurveType.BumpOut;
					break;
				case "rampdownpcurve":
					Type = PCurveType.RampDown;
					break;
				case "rampuppcurve":
					Type = PCurveType.RampUp;
					break;
				case "steppcurve":
					Type = PCurveType.Step;
					break;
				case "smoothpcurve":
					Type = PCurveType.Smooth;
					break;
				case "thornlpcurve":
					Type = PCurveType.ThornL;
					break;
				case "linearpcurve":
					Type = PCurveType.Linear;
					break;
				case "catmullrompcurve":
					Type = PCurveType.CatmullRom;
					break;
			}
			var points = (ThornTable)table["points"];
			Points = new List<Vector4>();
			for (int i = 1; i <= points.Length; i++) {
				var p = (ThornTable)points[i];
				var v = new Vector4((float)p[1], (float)p[2], (float)p[3], (float)p[4]);
				Points.Add(v);
			}
		}

        static float EvaluateFreeform(Vector4 pa, Vector4 pb, float time)
        {
            var period = (pb.X - pa.X);
            var aval = pa.Y;
            var bval = pb.Y;
            var bcontrol1 = pb.Z;
            var acontrol2 = pa.W;

            var x = (time - pa.X) / period;
            var x2 = x * x;
            var x3 = x2 * x;
            var _3x2 = 3 * x2;
            var _2x3 = 2 * x3;
            var _2x2 = 2 * x2;
            return (_3x2 - _2x3) * bval
                   + (_2x3 - _3x2 + 1) * aval
                   + (x3 - x2) * bcontrol1 * period
                   + (x3 - _2x2 + x) * acontrol2 * period;
        }

		public float GetValue(float time, float duration)
		{
            float x = 0;
			var p = Period / 1000f;
			if (p < float.Epsilon)
				x = time / duration;
			else
				x = (time % p) / p;
            Vector4 a = Vector4.Zero;
            Vector4 b = new Vector4(1,1,0,0);
            if (Points.Count >= 2)
            {
                if (x <= 0)
                    return Points[0].Y;
                if (x >= 1)
                    return Points[Points.Count - 1].Y;
                //X - time, Y - value, Z - in, W - out

                for (int i = 0; i < Points.Count - 1; i++)
                {
                    if (x >= Points[i].X && x <= Points[i + 1].X)
                    {
                        a = Points[i];
                        b = Points[i + 1];
                    }
                }
            }

            switch (Type)
			{
                case PCurveType.FreeForm:
                    return EvaluateFreeform(a, b, x);
				case PCurveType.Step:
                    return a.Y;
                case PCurveType.BumpIn:
                case PCurveType.RampUp:
                    return Easing.Ease(EasingTypes.EaseIn, x, a.X, b.X, a.Y, b.Y);
                case PCurveType.BumpOut:
                case PCurveType.RampDown:
                    return Easing.Ease(EasingTypes.EaseOut, x, a.X, b.X, a.Y, b.Y);
                case PCurveType.Smooth:
                    return Easing.Ease(EasingTypes.EaseInOut, x, a.X, b.Y, a.Y, b.Y);
                default:
                    return Easing.Ease(EasingTypes.Linear, x, a.X, b.X, a.Y, b.Y);
            }
        }
	}
}
