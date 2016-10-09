/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
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
		Dictionary<Vector2, BezierFunction> beziers =new Dictionary<Vector2, BezierFunction>();
		float tmin, tmax;
		public float GetValue(float time) {
			if (Keyframes == null)
				return Value;
			if (Keyframes.Count == 1)
				return Keyframes [0].Value;
			if (beziers == null)
				GenerateBezierFunctions ();
			if (time <= tmin)
				return Keyframes[0].Value;
			if (time >= tmax)
			{
				switch (Flags)
				{
					case LoopFlags.PlayOnce:
						return Keyframes[Keyframes.Count - 1].Value;
				}
				return Keyframes[Keyframes.Count - 1].Value;
			}
			for (int i = 0; i < Keyframes.Count - 1; i++)
			{
				var a = Keyframes[i];
				var b = Keyframes[i + 1];
				if (time < b.Time && time > a.Time)
				{
					var t = (time - a.Time) / (b.Time - a.Time);
					var amount = beziers[new Vector2(a.Time, b.Time)](t);
					return AlchemyEasing.Ease(EasingTypes.Linear, (float)amount, 0, 1, a.Value, b.Value);
				}
			}
			throw new Exception("Malformed AlchemyCurve");
		}

		void GenerateBezierFunctions()
		{
			foreach (var pair in Keyframes.Zip(Keyframes.Skip(1), (a, b) => new[] { a, b })) {
				GenerateFunction (pair [0], pair [1]);
			}
			tmin = Keyframes[0].Time;
			tmax = Keyframes[Keyframes.Count - 1].Time;
		}

		void GenerateFunction(CurveKeyframe a, CurveKeyframe b)
		{
			var key = new Vector2 (a.Time, b.Time);
			if (beziers.ContainsKey (key))
				return;
			float p1y = AlchemyEasing.Ease(EasingTypes.Linear, b.InTangent, a.Value, b.Value, 0, 1);

			float p2y = AlchemyEasing.Ease(EasingTypes.Linear, a.OutTangent, a.Value, b.Value, 0, 1);
			beziers.Add (key, CubicBezier.Bezier (0.5, p1y, 0.5, 1 - p2y));
		}
	}
}

